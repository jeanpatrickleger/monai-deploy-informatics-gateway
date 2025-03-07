﻿// SPDX-FileCopyrightText: © 2021-2022 MONAI Consortium
// SPDX-FileCopyrightText: © 2019-2021 NVIDIA Corporation
// SPDX-License-Identifier: Apache License 2.0

using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FellowOakDicom;
using FellowOakDicom.Imaging.Codec;
using FellowOakDicom.Log;
using FellowOakDicom.Network;
using Monai.Deploy.InformaticsGateway.Api;
using Monai.Deploy.InformaticsGateway.Common;
using Monai.Deploy.InformaticsGateway.Logging;

namespace Monai.Deploy.InformaticsGateway.Services.Scp
{
    /// <summary>
    /// A new instance of <c>ScpServiceInternal</c> is created for every new association.
    /// </summary>
    internal class ScpServiceInternal :
        DicomService,
        IDicomServiceProvider,
        IDicomCEchoProvider,
        IDicomCStoreProvider
    {
        private const int ERROR_HANDLE_DISK_FULL = 0x27;
        private const int ERROR_DISK_FULL = 0x70;

        private Microsoft.Extensions.Logging.ILogger _logger;
        private IApplicationEntityManager _associationDataProvider;
        private IDisposable _loggerScope;
        private Guid _associationId;

        public ScpServiceInternal(INetworkStream stream, Encoding fallbackEncoding, FellowOakDicom.Log.ILogger log, ILogManager logManager, INetworkManager network, ITranscoderManager transcoder)
                : base(stream, fallbackEncoding, log, logManager, network, transcoder)
        {
        }

        public Task<DicomCEchoResponse> OnCEchoRequestAsync(DicomCEchoRequest request)
        {
            _logger?.CEchoReceived();
            return Task.FromResult(new DicomCEchoResponse(request, DicomStatus.Success));
        }

        public void OnConnectionClosed(Exception exception)
        {
            if (exception != null)
            {
                _logger?.ConnectionClosedWithException(exception);
            }

            _loggerScope?.Dispose();
            Interlocked.Decrement(ref ScpService.ActiveConnections);
        }

        public async Task<DicomCStoreResponse> OnCStoreRequestAsync(DicomCStoreRequest request)
        {
            try
            {
                _logger?.TransferSyntaxUsed(request.TransferSyntax);
                await _associationDataProvider.HandleCStoreRequest(request, Association.CalledAE, Association.CallingAE, _associationId).ConfigureAwait(false);
                return new DicomCStoreResponse(request, DicomStatus.Success);
            }
            catch (InsufficientStorageAvailableException ex)
            {
                _logger?.CStoreFailedWithNoSpace(ex);
                return new DicomCStoreResponse(request, DicomStatus.ResourceLimitation);
            }
            catch (System.IO.IOException ex) when ((ex.HResult & 0xFFFF) == ERROR_HANDLE_DISK_FULL || (ex.HResult & 0xFFFF) == ERROR_DISK_FULL)
            {
                _logger?.CStoreFailedWithNoSpace(ex);
                return new DicomCStoreResponse(request, DicomStatus.ResourceLimitation);
            }
            catch (Exception ex)
            {
                _logger?.CStoreFailed(ex);
                return new DicomCStoreResponse(request, DicomStatus.ProcessingFailure);
            }
        }

        public Task OnCStoreRequestExceptionAsync(string tempFileName, Exception e)
        {
            _logger?.CStoreFailed(e);
            return Task.CompletedTask;
        }

        public void OnReceiveAbort(DicomAbortSource source, DicomAbortReason reason)
        {
            _logger?.CStoreAbort(source, reason);
        }

        /// <summary>
        /// Start timer only if a receive association release request is received.
        /// </summary>
        /// <returns></returns>
        public Task OnReceiveAssociationReleaseRequestAsync()
        {
            _logger?.CStoreAssociationReleaseRequest();
            return SendAssociationReleaseResponseAsync();
        }

        public Task OnReceiveAssociationRequestAsync(DicomAssociation association)
        {
            Interlocked.Increment(ref ScpService.ActiveConnections);
            _associationDataProvider = UserState as IApplicationEntityManager;

            if (_associationDataProvider is null)
            {
                throw new ServiceException($"{nameof(UserState)} must be an instance of IAssociationDataProvider");
            }

            _logger = _associationDataProvider.GetLogger(Association.CalledAE);

            _associationId = Guid.NewGuid();
            var associationIdStr = $"#{_associationId} {association.RemoteHost}:{association.RemotePort}";

            _loggerScope = _logger?.BeginScope(new LoggingDataDictionary<string, object> { { "Association", associationIdStr } });
            _logger?.CStoreAssociationReceived(association.RemoteHost, association.RemotePort);

            if (!IsValidSourceAe(association.CallingAE, association.RemoteHost))
            {
                return SendAssociationRejectAsync(
                    DicomRejectResult.Permanent,
                    DicomRejectSource.ServiceUser,
                    DicomRejectReason.CallingAENotRecognized);
            }

            if (!IsValidCalledAe(association.CalledAE))
            {
                return SendAssociationRejectAsync(
                    DicomRejectResult.Permanent,
                    DicomRejectSource.ServiceUser,
                    DicomRejectReason.CalledAENotRecognized);
            }

            foreach (var pc in association.PresentationContexts)
            {
                if (pc.AbstractSyntax == DicomUID.Verification)
                {
                    if (!_associationDataProvider.Configuration.Value.Dicom.Scp.EnableVerification)
                    {
                        _logger?.VerificationServiceDisabled();
                        return SendAssociationRejectAsync(
                            DicomRejectResult.Permanent,
                            DicomRejectSource.ServiceUser,
                            DicomRejectReason.ApplicationContextNotSupported
                        );
                    }
                    pc.AcceptTransferSyntaxes(_associationDataProvider.Configuration.Value.Dicom.Scp.VerificationServiceTransferSyntaxes.ToDicomTransferSyntaxArray());
                }
                else if (pc.AbstractSyntax.StorageCategory != DicomStorageCategory.None)
                {
                    if (!_associationDataProvider.CanStore)
                    {
                        return SendAssociationRejectAsync(
                            DicomRejectResult.Permanent,
                            DicomRejectSource.ServiceUser,
                            DicomRejectReason.NoReasonGiven);
                    }
                    // Accept any proposed TS
                    pc.AcceptTransferSyntaxes(pc.GetTransferSyntaxes().ToArray());
                }
            }

            return SendAssociationAcceptAsync(association);
        }

        private bool IsValidCalledAe(string calledAe)
        {
            return _associationDataProvider.IsAeTitleConfigured(calledAe);
        }

        private bool IsValidSourceAe(string callingAe, string host)
        {
            if (!_associationDataProvider.Configuration.Value.Dicom.Scp.RejectUnknownSources) return true;

            return _associationDataProvider.IsValidSource(callingAe, host);
        }
    }
}
