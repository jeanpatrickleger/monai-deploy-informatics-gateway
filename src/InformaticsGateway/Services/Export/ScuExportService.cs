﻿// SPDX-FileCopyrightText: © 2021-2022 MONAI Consortium
// SPDX-FileCopyrightText: © 2019-2021 NVIDIA Corporation
// SPDX-License-Identifier: Apache License 2.0

using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Ardalis.GuardClauses;
using FellowOakDicom.Network;
using FellowOakDicom.Network.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Monai.Deploy.InformaticsGateway.Api;
using Monai.Deploy.InformaticsGateway.Common;
using Monai.Deploy.InformaticsGateway.Configuration;
using Monai.Deploy.InformaticsGateway.Logging;
using Monai.Deploy.InformaticsGateway.Repositories;
using Monai.Deploy.InformaticsGateway.Services.Storage;
using Polly;

namespace Monai.Deploy.InformaticsGateway.Services.Export
{
    internal class ScuExportService : ExportServiceBase
    {
        private readonly ILogger<ScuExportService> _logger;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly IOptions<InformaticsGatewayConfiguration> _configuration;
        private readonly IDicomToolkit _dicomToolkit;

        protected override int Concurrency { get; }
        public override string RoutingKey { get; }
        public override string ServiceName => "DICOM Export Service";

        public ScuExportService(
            ILogger<ScuExportService> logger,
            IServiceScopeFactory serviceScopeFactory,
            IOptions<InformaticsGatewayConfiguration> configuration,
            IStorageInfoProvider storageInfoProvider,
            IDicomToolkit dicomToolkit)
            : base(logger, configuration, serviceScopeFactory, storageInfoProvider)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _serviceScopeFactory = serviceScopeFactory ?? throw new ArgumentNullException(nameof(serviceScopeFactory));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _dicomToolkit = dicomToolkit ?? throw new ArgumentNullException(nameof(dicomToolkit));

            RoutingKey = $"{configuration.Value.Messaging.Topics.ExportRequestPrefix}.{_configuration.Value.Dicom.Scu.AgentName}";
            Concurrency = _configuration.Value.Dicom.Scu.MaximumNumberOfAssociations;
        }

        protected override async Task<ExportRequestDataMessage> ExportDataBlockCallback(ExportRequestDataMessage exportRequestData, CancellationToken cancellationToken)
        {
            using var loggerScope = _logger.BeginScope(new LoggingDataDictionary<string, object> { { "ExportTaskId", exportRequestData.ExportTaskId }, { "CorrelationId", exportRequestData.CorrelationId }, { "Filename", exportRequestData.Filename } });

            var manualResetEvent = new ManualResetEvent(false);
            IDicomClient client = null;
            DestinationApplicationEntity destination = null;
            try
            {
                destination = LookupDestination(exportRequestData);
            }
            catch (ConfigurationException ex)
            {
                _logger.ScuExportConfigurationError(ex.Message, ex);
                exportRequestData.SetFailed(ex.Message);
                return exportRequestData;
            }

            try
            {
                client = DicomClientFactory.Create(
                    destination.HostIp,
                    destination.Port,
                    false,
                    _configuration.Value.Dicom.Scu.AeTitle,
                    destination.AeTitle);

                client.AssociationAccepted += (sender, args) => _logger.AssociationAccepted();
                client.AssociationRejected += (sender, args) => _logger.AssociationRejected();
                client.AssociationReleased += (sender, args) => _logger.AssociationReleased();
                client.ServiceOptions.LogDataPDUs = _configuration.Value.Dicom.Scu.LogDataPdus;
                client.ServiceOptions.LogDimseDatasets = _configuration.Value.Dicom.Scu.LogDimseDatasets;

                client.NegotiateAsyncOps();
                if (GenerateRequests(exportRequestData, client, manualResetEvent))
                {
                    await Policy
                       .Handle<Exception>()
                       .WaitAndRetryAsync(
                           _configuration.Value.Export.Retries.RetryDelays,
                           (exception, timeSpan, retryCount, context) =>
                           {
                               _logger.DimseExportErrorWithRetry(timeSpan, retryCount, exception);
                           })
                       .ExecuteAsync(async () =>
                       {
                           _logger.DimseExporting(destination.AeTitle, destination.HostIp, destination.Port);
                           await client.SendAsync(cancellationToken).ConfigureAwait(false);
                           manualResetEvent.WaitOne();
                           _logger.DimseExportComplete(destination.AeTitle);
                       }).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                HandleCStoreException(ex, exportRequestData);
            }

            return exportRequestData;
        }

        private DestinationApplicationEntity LookupDestination(ExportRequestDataMessage exportRequestData)
        {
            Guard.Against.Null(exportRequestData, nameof(exportRequestData));

            if (string.IsNullOrWhiteSpace(exportRequestData.Destination))
            {
                throw new ConfigurationException("Export task does not have destination set.");
            }

            using var scope = _serviceScopeFactory.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IInformaticsGatewayRepository<DestinationApplicationEntity>>();
            var destination = repository.FirstOrDefault(p => p.Name.Equals(exportRequestData.Destination, StringComparison.InvariantCultureIgnoreCase));

            if (destination is null)
            {
                throw new ConfigurationException($"Specified destination '{exportRequestData.Destination}' does not exist.");
            }

            return destination;
        }

        private bool GenerateRequests(
            ExportRequestDataMessage exportRequestData,
            IDicomClient client,
            ManualResetEvent manualResetEvent)
        {
            try
            {
                var dicomFile = _dicomToolkit.Load(exportRequestData.FileContent);

                var request = new DicomCStoreRequest(dicomFile);

                request.OnResponseReceived += (req, response) =>
                {
                    if (response.Status == DicomStatus.Success)
                    {
                        _logger.DimseExportInstanceComplete();
                    }
                    else
                    {
                        var errorMessage = $"Failed to export with error {response.Status}";
                        _logger.DimseExportInstanceError(response.Status);
                        exportRequestData.SetFailed(errorMessage);
                    }
                    manualResetEvent.Set();
                };

                client.AddRequestAsync(request).ConfigureAwait(false);
                return true;
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error while adding DICOM C-STORE request: {exception.Message}";
                _logger.DimseExportErrorAddingInstance(exception.Message, exception);
                exportRequestData.SetFailed(errorMessage);
                return false;
            }
        }

        private void HandleCStoreException(Exception ex, ExportRequestDataMessage exportRequestData)
        {
            var exception = ex;

            if (exception is AggregateException)
            {
                exception = exception.InnerException;
            }

            var errorMessage = $"Job failed with error: {exception.Message}.";

            if (exception is DicomAssociationAbortedException abortEx)
            {
                errorMessage = $"Association aborted with reason {abortEx.AbortReason}.";
            }
            else if (exception is DicomAssociationRejectedException rejectEx)
            {
                errorMessage = $"Association rejected with reason {rejectEx.RejectReason}.";
            }
            else if (exception is SocketException socketException)
            {
                errorMessage = $"Association aborted with error {socketException.Message}.";
            }

            _logger.ExportException(errorMessage, ex);
            exportRequestData.SetFailed(errorMessage);
        }
    }
}
