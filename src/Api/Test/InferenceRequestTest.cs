﻿// SPDX-FileCopyrightText: © 2021-2022 MONAI Consortium
// SPDX-FileCopyrightText: © 2019-2021 NVIDIA Corporation
// SPDX-License-Identifier: Apache License 2.0

using System;
using System.Collections.Generic;
using Monai.Deploy.InformaticsGateway.Api.Rest;
using Xunit;

namespace Monai.Deploy.InformaticsGateway.Api.Test
{
    public class InferenceRequestTest
    {
        [Fact(DisplayName = "Algorithm shall return null if no algorithm is defined")]
        public void Algorithm_ShallReturnNullOnNoMatch()
        {
            var request = new InferenceRequest();

            Assert.Null(request.Application);
        }

        [Fact(DisplayName = "Algorithm shall return defined algorithm")]
        public void Algorithm_ShallReturnAlgorithm()
        {
            var request = new InferenceRequest();
            request.InputResources.Add(new RequestInputDataResource { Interface = InputInterfaceType.DicomWeb });
            request.InputResources.Add(new RequestInputDataResource { Interface = InputInterfaceType.Algorithm, ConnectionDetails = new InputConnectionDetails() });
            request.InputResources.Add(new RequestInputDataResource { Interface = InputInterfaceType.Dimse });

            Assert.NotNull(request.Application);
        }

        [Fact(DisplayName = "IsValid shall return all errors")]
        public void IsValid_ShallReturnAllErrors()
        {
            var request = new InferenceRequest();
            Assert.False(request.IsValid(out var _));

            request.InputResources.Add(new RequestInputDataResource { Interface = InputInterfaceType.Algorithm });
            Assert.False(request.IsValid(out var _));
        }

        [Fact(DisplayName = "IsValid shall return false if no studies were defined")]
        public void IsValid_ShallReturnFalseWithEmptyStudy()
        {
            var request = new InferenceRequest();
            request.InputResources.Add(new RequestInputDataResource
            {
                Interface = InputInterfaceType.Algorithm,
                ConnectionDetails = new InputConnectionDetails()
            });
            request.InputResources.Add(new RequestInputDataResource
            {
                Interface = InputInterfaceType.DicomWeb,
                ConnectionDetails = new InputConnectionDetails
                {
                    Uri = "http://this.is.not.a.valid.uri\\",
                    AuthId = "token",
                    AuthType = ConnectionAuthType.Bearer,
                }
            });
            request.InputMetadata = new InferenceRequestMetadata
            {
                Details = new InferenceRequestDetails
                {
                    Type = InferenceRequestType.DicomUid
                }
            };
            Assert.False(request.IsValid(out var _));
        }

        [Fact(DisplayName = "IsValid shall return false if study contains no UID")]
        public void IsValid_ShallReturnFalseWithEmptyStudyInstanceUid()
        {
            var request = new InferenceRequest();
            request.InputResources.Add(new RequestInputDataResource
            {
                Interface = InputInterfaceType.Algorithm,
                ConnectionDetails = new InputConnectionDetails()
            });
            request.InputResources.Add(new RequestInputDataResource
            {
                Interface = InputInterfaceType.DicomWeb,
                ConnectionDetails = new InputConnectionDetails
                {
                    Uri = "http://this.is.not.a.valid.uri\\",
                    AuthId = "token",
                    AuthType = ConnectionAuthType.Bearer,
                }
            });
            request.InputMetadata = new InferenceRequestMetadata
            {
                Details = new InferenceRequestDetails
                {
                    Type = InferenceRequestType.DicomUid,
                    Studies = new List<RequestedStudy>()
                     {
                        new RequestedStudy()
                     {
                     }
                }
                }
            };
            Assert.False(request.IsValid(out var _));
        }

        [Fact(DisplayName = "IsValid shall return false if series contains no UID")]
        public void IsValid_ShallReturnFalseWithEmptySeriesInstanceUid()
        {
            var request = new InferenceRequest();
            request.InputResources.Add(new RequestInputDataResource
            {
                Interface = InputInterfaceType.Algorithm,
                ConnectionDetails = new InputConnectionDetails()
            });
            request.InputResources.Add(new RequestInputDataResource
            {
                Interface = InputInterfaceType.DicomWeb,
                ConnectionDetails = new InputConnectionDetails
                {
                    Uri = "http://this.is.not.a.valid.uri\\",
                    AuthId = "token",
                    AuthType = ConnectionAuthType.Bearer,
                }
            });
            request.InputMetadata = new InferenceRequestMetadata
            {
                Details = new InferenceRequestDetails
                {
                    Type = InferenceRequestType.DicomUid,
                    Studies = new List<RequestedStudy>()
                     {
                        new RequestedStudy()
                     {
                             StudyInstanceUid = "123",
                              Series = new List<RequestedSeries>()
                              {
                                  new RequestedSeries()
                                  {
                                  }
                              }
                     }
                }
                }
            };
            Assert.False(request.IsValid(out var _));
        }

        [Fact(DisplayName = "IsValid shall return false if instance contains no instance")]
        public void IsValid_ShallReturnFalseWithEmptySopInstanceUid()
        {
            var request = new InferenceRequest();
            request.InputResources.Add(new RequestInputDataResource
            {
                Interface = InputInterfaceType.Algorithm,
                ConnectionDetails = new InputConnectionDetails()
            });
            request.InputResources.Add(new RequestInputDataResource
            {
                Interface = InputInterfaceType.DicomWeb,
                ConnectionDetails = new InputConnectionDetails
                {
                    Uri = "http://this.is.not.a.valid.uri\\",
                    AuthId = "token",
                    AuthType = ConnectionAuthType.Bearer,
                }
            });
            request.InputMetadata = new InferenceRequestMetadata
            {
                Details = new InferenceRequestDetails
                {
                    Type = InferenceRequestType.DicomUid,
                    Studies = new List<RequestedStudy>()
                     {
                        new RequestedStudy()
                        {
                             StudyInstanceUid = "123",
                              Series = new List<RequestedSeries>()
                              {
                                  new RequestedSeries()
                                  {
                                       SeriesInstanceUid = "123",
                                        Instances = new List<RequestedInstance>()
                                        {
                                            new RequestedInstance()
                                            {
                                            }
                                        }
                                  }
                              }
                         }
                    }
                }
            };
            Assert.False(request.IsValid(out var _));
        }

        [Fact(DisplayName = "IsValid shall return false if missing patient ID")]
        public void IsValid_ShallReturnFalseWithoutPatientId()
        {
            var request = new InferenceRequest();
            request.InputResources.Add(new RequestInputDataResource
            {
                Interface = InputInterfaceType.Algorithm,
                ConnectionDetails = new InputConnectionDetails()
            });
            request.InputResources.Add(new RequestInputDataResource
            {
                Interface = InputInterfaceType.DicomWeb,
                ConnectionDetails = new InputConnectionDetails
                {
                    Uri = "http://this.is.not.a.valid.uri\\",
                    AuthId = "token",
                    AuthType = ConnectionAuthType.Bearer,
                }
            });
            request.InputMetadata = new InferenceRequestMetadata
            {
                Details = new InferenceRequestDetails
                {
                    Type = InferenceRequestType.DicomPatientId
                }
            };
            Assert.False(request.IsValid(out var _));
        }

        [Fact(DisplayName = "IsValid shall return false if no accession number were defined")]
        public void IsValid_ShallReturnFalseWithoutAccessNumbers()
        {
            var request = new InferenceRequest();
            request.InputResources.Add(new RequestInputDataResource
            {
                Interface = InputInterfaceType.Algorithm,
                ConnectionDetails = new InputConnectionDetails()
            });
            request.InputResources.Add(new RequestInputDataResource
            {
                Interface = InputInterfaceType.DicomWeb,
                ConnectionDetails = new InputConnectionDetails
                {
                    Uri = "http://this.is.not.a.valid.uri\\",
                    AuthId = "token",
                    AuthType = ConnectionAuthType.Bearer,
                }
            });
            request.InputMetadata = new InferenceRequestMetadata
            {
                Details = new InferenceRequestDetails
                {
                    Type = InferenceRequestType.AccessionNumber
                }
            };
            Assert.False(request.IsValid(out var _));
        }

        [Fact(DisplayName = "IsValid shall return false for unknown request type")]
        public void IsValid_ShallReturnFalseForUnknownRequestType()
        {
            var request = new InferenceRequest();
            request.InputResources.Add(new RequestInputDataResource
            {
                Interface = InputInterfaceType.Algorithm,
                ConnectionDetails = new InputConnectionDetails()
            });
            request.InputResources.Add(new RequestInputDataResource
            {
                Interface = InputInterfaceType.DicomWeb,
                ConnectionDetails = new InputConnectionDetails
                {
                    Uri = "http://this.is.not.a.valid.uri\\",
                    AuthId = "token",
                    AuthType = ConnectionAuthType.Bearer,
                }
            });
            request.InputMetadata = new InferenceRequestMetadata
            {
                Details = new InferenceRequestDetails()
            };
            Assert.False(request.IsValid(out var _));
        }

        [Fact(DisplayName = "IsValid shall return false without a valid credential")]
        public void IsValid_ShallReturnFalseWithoutAValidCredential()
        {
            var request = new InferenceRequest();
            request.InputResources.Add(new RequestInputDataResource
            {
                Interface = InputInterfaceType.Algorithm,
                ConnectionDetails = new InputConnectionDetails()
            });
            request.InputResources.Add(new RequestInputDataResource
            {
                Interface = InputInterfaceType.DicomWeb,
                ConnectionDetails = new InputConnectionDetails
                {
                    Uri = "http://this.is.not.a.valid.uri\\",
                    AuthType = ConnectionAuthType.Bearer,
                }
            });
            request.InputMetadata = new InferenceRequestMetadata
            {
                Details = new InferenceRequestDetails()
            };
            Assert.False(request.IsValid(out var _));
        }

        [Fact(DisplayName = "IsValid shall return false with invalid uri")]
        public void IsValid_ShallReturnFalseWithInvalidUri()
        {
            var request = new InferenceRequest();
            request.InputResources.Add(new RequestInputDataResource
            {
                Interface = InputInterfaceType.Algorithm,
                ConnectionDetails = new InputConnectionDetails()
            });
            request.InputResources.Add(new RequestInputDataResource
            {
                Interface = InputInterfaceType.DicomWeb,
                ConnectionDetails = new InputConnectionDetails
                {
                    Uri = "http://this.is.not.a.valid.uri\\",
                    AuthId = "token",
                    AuthType = ConnectionAuthType.Bearer,
                }
            });
            request.InputMetadata = new InferenceRequestMetadata
            {
                Details = new InferenceRequestDetails
                {
                    Type = InferenceRequestType.DicomUid,
                    Studies = new List<RequestedStudy>
                    {
                        new RequestedStudy
                        {
                            StudyInstanceUid = "1"
                        }
                    }
                }
            };
            Assert.False(request.IsValid(out var _));
        }

        [Fact(DisplayName = "IsValid shall return false with missing TransactionId")]
        public void IsValid_ShallReturnFalseWithEmptyTransactionId()
        {
            var request = MockGoodRequest();
            request.TransactionId = "";
            Assert.False(request.IsValid(out var _));
        }

        [Fact(DisplayName = "IsValid shall return false with no resource defined for FHIR input")]
        public void IsValid_ShallReturnFalsWithNoResourceInFhirInput()
        {
            var request = MockGoodRequest();
            request.InputMetadata.Details = new InferenceRequestDetails()
            {
                Type = InferenceRequestType.FhireResource
            };

            Assert.False(request.IsValid(out var _));
        }

        [Fact(DisplayName = "IsValid shall return false with no resource type for a FHIR resource")]
        public void IsValid_ShallReturnFalsWithNoResourceTypeForFhirResource()
        {
            var request = MockGoodRequest();
            request.InputMetadata.Details = new InferenceRequestDetails()
            {
                Type = InferenceRequestType.FhireResource,
                Resources = new List<FhirResource>()
                {
                    new FhirResource(){}
                }
            };

            Assert.False(request.IsValid(out var _));
        }

        [Fact(DisplayName = "IsValid shall return false with malformed input FHIR URI")]
        public void IsValid_ShallReturnFalsWithBadUriInFhirInputUri()
        {
            var request = MockGoodRequest();

            request.InputResources.Add(new RequestInputDataResource
            {
                Interface = InputInterfaceType.Fhir,
                ConnectionDetails = new InputConnectionDetails
                {
                    Uri = "http://this.is.not.a/valid/uri\\",
                    AuthId = "token",
                    AuthType = ConnectionAuthType.Bearer
                }
            });

            Assert.False(request.IsValid(out var _));
        }

        [Fact(DisplayName = "IsValid shall return false with malformed output FHIR URI")]
        public void IsValid_ShallReturnFalsWithBadUriInFhirOutputUri()
        {
            var request = MockGoodRequest();

            request.OutputResources.Add(new RequestOutputDataResource
            {
                Interface = InputInterfaceType.Fhir,
                ConnectionDetails = new InputConnectionDetails
                {
                    Uri = "http://this.is.not.a/valid/uri\\",
                    AuthId = "token",
                    AuthType = ConnectionAuthType.Bearer
                }
            });

            Assert.False(request.IsValid(out var _));
        }

        [Fact(DisplayName = "IsValid shall return true with valid request")]
        public void IsValid_ShallReturnTrue()
        {
            var request = MockGoodRequest();
            Assert.True(request.IsValid(out var _));
        }

        private static InferenceRequest MockGoodRequest()
        {
            var request = new InferenceRequest
            {
                TransactionId = Guid.NewGuid().ToString()
            };
            request.InputResources.Add(new RequestInputDataResource
            {
                Interface = InputInterfaceType.Algorithm,
                ConnectionDetails = new InputConnectionDetails()
            });
            request.InputResources.Add(new RequestInputDataResource
            {
                Interface = InputInterfaceType.DicomWeb,
                ConnectionDetails = new InputConnectionDetails
                {
                    Uri = "http://this.is.a/valid/uri",
                    AuthId = "token",
                    AuthType = ConnectionAuthType.Bearer
                }
            });
            request.OutputResources.Add(new RequestOutputDataResource
            {
                Interface = InputInterfaceType.DicomWeb,
                ConnectionDetails = new DicomWebConnectionDetails
                {
                    Uri = "http://this.is.a/valid/uri",
                    AuthId = "token",
                    AuthType = ConnectionAuthType.Bearer
                }
            });
            request.InputMetadata = new InferenceRequestMetadata
            {
                Inputs = new List<InferenceRequestDetails>
                {
                    new InferenceRequestDetails
                    {
                        Type = InferenceRequestType.DicomUid,
                        Studies = new List<RequestedStudy>
                        {
                            new RequestedStudy
                            {
                                StudyInstanceUid = "1"
                            }
                        }
                    },
                    new InferenceRequestDetails
                    {
                        Type = InferenceRequestType.FhireResource,
                        Resources = new List<FhirResource>()
                        {
                            new FhirResource()
                            {
                                 Type = "Patient",
                                 Id = "123"
                            }
                        }
                    }
                }
            };
            return request;
        }

        [Fact(DisplayName = "ConfigureTemporaryStorageLocation shall throw when input is invalid")]
        public void ConfigureTemporaryStorageLocation_ShallThrowWithInvalidInput()
        {
            var request = new InferenceRequest();

            Assert.Throws<ArgumentNullException>(() =>
            {
                request.ConfigureTemporaryStorageLocation(null);
            });
            Assert.Throws<ArgumentException>(() =>
            {
                request.ConfigureTemporaryStorageLocation(" ");
            });
        }

        [Fact(DisplayName = "ConfigureTemporaryStorageLocation shall throw if already configured")]
        public void ConfigureTemporaryStorageLocation_ShallThrowIfAlreadyConfigured()
        {
            var request = new InferenceRequest();
            request.ConfigureTemporaryStorageLocation("/blabla");

            Assert.Throws<InferenceRequestException>(() =>
            {
                request.ConfigureTemporaryStorageLocation("/new-location");
            });
        }
    }
}
