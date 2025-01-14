﻿// SPDX-FileCopyrightText: © 2021-2022 MONAI Consortium
// SPDX-License-Identifier: Apache License 2.0

using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FellowOakDicom;
using FellowOakDicom.Imaging.Codec;
using FellowOakDicom.Log;
using FellowOakDicom.Network;
using Microsoft.Extensions.Logging;

namespace Monai.Deploy.InformaticsGateway.SharedTest
{
    public class DicomScpFixture : IDisposable
    {
        internal static string s_aETITLE = "STORESCP";
        private IDicomServer _server;
        private bool _disposedValue;

        public static DicomStatus DicomStatus { get; set; } = DicomStatus.Success;
        public static Microsoft.Extensions.Logging.ILogger Logger { get; set; }

        public DicomScpFixture()
        {
        }

        public void Start(int port = 11104)
        {
            if (_server is null)
            {
                _server = DicomServerFactory.Create<CStoreScp>(
                    NetworkManager.IPv4Any,
                    port);

                if (_server.Exception is not null)
                {
                    throw _server.Exception;
                }
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _server?.Dispose();
                    _server = null;
                }

                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }

    public class CStoreScp : DicomService, IDicomServiceProvider, IDicomCStoreProvider, IDicomCEchoProvider
    {
        public CStoreScp(INetworkStream stream, Encoding fallbackEncoding, FellowOakDicom.Log.ILogger log, ILogManager logManager, INetworkManager network, ITranscoderManager transcoder)
                : base(stream, fallbackEncoding, log, logManager, network, transcoder)
        {
        }

        public Task OnReceiveAssociationRequestAsync(DicomAssociation association)
        {
            if (association.CalledAE == "ABORT")
            {
                return SendAbortAsync(DicomAbortSource.ServiceUser, DicomAbortReason.NotSpecified);
            }

            if (association.CalledAE != DicomScpFixture.s_aETITLE)
            {
                return SendAssociationRejectAsync(
                    DicomRejectResult.Permanent,
                    DicomRejectSource.ServiceUser,
                    DicomRejectReason.CalledAENotRecognized);
            }

            foreach (var pc in association.PresentationContexts)
            {
                if (pc.AbstractSyntax == DicomUID.Verification) pc.AcceptTransferSyntaxes(pc.GetTransferSyntaxes().ToArray());
                else if (pc.AbstractSyntax.StorageCategory != DicomStorageCategory.None) pc.AcceptTransferSyntaxes(pc.GetTransferSyntaxes().ToArray());
            }

            return SendAssociationAcceptAsync(association);
        }

        public Task OnReceiveAssociationReleaseRequestAsync()
        {
            return SendAssociationReleaseResponseAsync();
        }

        public void OnReceiveAbort(DicomAbortSource source, DicomAbortReason reason)
        {
            // ignore
        }

        public void OnConnectionClosed(Exception exception)
        {
            // ignore
        }

        public Task<DicomCStoreResponse> OnCStoreRequestAsync(DicomCStoreRequest request)
        {
            DicomScpFixture.Logger.LogInformation($"Instance received {request.SOPInstanceUID.UID}");
            return Task.FromResult(new DicomCStoreResponse(request, DicomScpFixture.DicomStatus));
        }

        public Task OnCStoreRequestExceptionAsync(string tempFileName, Exception e)
        {
            return Task.CompletedTask;
        }

        public Task<DicomCEchoResponse> OnCEchoRequestAsync(DicomCEchoRequest request)
        {
            return Task.FromResult(new DicomCEchoResponse(request, DicomStatus.Success));
        }
    }
}
