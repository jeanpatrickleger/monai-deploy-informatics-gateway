// SPDX-FileCopyrightText: � 2021-2022 MONAI Consortium
// SPDX-FileCopyrightText: � 2019-2021 NVIDIA Corporation
// SPDX-License-Identifier: Apache License 2.0

using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Monai.Deploy.InformaticsGateway.Api.Rest
{
    /// <summary>
    /// Details of an inference request.
    /// </summary>
    /// <example>
    /// <code>
    /// {
    ///     ...
    ///     "details" : {
    ///         "type": "DICOM_UID",
    ///         "studies": [ ... ]
    ///     }
    ///     ...
    /// } or
    ///
    /// {
    ///     ...
    ///     "details" : {
    ///         "type": "DICOM_PATIENT_ID",
    ///         "PatientID": "..."
    ///     }
    ///     ...
    /// } or
    ///
    /// {
    ///     ...
    ///     "details" : {
    ///         "type": "ACCESSION_NUMBER",
    ///         "accessionNumber": [ ... ]
    ///     }
    ///     ...
    /// } or
    ///
    /// {
    ///     ...
    ///     "details" : {
    ///         "type": "FHIR_RESOURCE",
    ///         "resources": [ ... ]
    ///     }
    ///     ...
    /// }
    /// </code>
    /// </example>
    /// <remarks>
    /// <para><c>type></c> is required.</para>
    /// <para><c>PatientID></c> is required if <c>type</c> is <see cref="Monai.Deploy.InformaticsGateway.Api.Rest.InferenceRequestType.DicomUid" />.</para>
    /// <para><c>studies></c> is required if <c>type</c> is <see cref="Monai.Deploy.InformaticsGateway.Api.Rest.InferenceRequestType.DicomPatientId" />.</para>
    /// <para><c>accessionNumber></c> is required if <c>type</c> is <see cref="Monai.Deploy.InformaticsGateway.Api.Rest.InferenceRequestType.AccessionNumber" />.</para>
    /// </remarks>
    public class InferenceRequestDetails
    {
        private string _fhirAcceptHeader;

        /// <summary>
        /// Gets or sets the type of the inference request.
        /// </summary>
        [JsonConverter(typeof(StringEnumConverter))]
        [JsonProperty(PropertyName = "type")]
        public InferenceRequestType Type { get; set; }

        /// <summary>
        /// Gets or sets the DICOM studies to be retrieved.
        /// Used when <c>Type</c> is <see cref="T:Monai.Deploy.InformaticsGateway.Api.Rest.InferenceRequestType.DicomUid" />.
        /// </summary>
        [JsonProperty(PropertyName = "studies")]
        public IList<RequestedStudy> Studies { get; set; }

        /// <summary>
        /// Gets or sets Patient ID that is used to query the data source.
        /// Used when <c>Type</c> is <see cref="T:Monai.Deploy.InformaticsGateway.Api.Rest.InferenceRequestType.DicomPatientId" />.
        /// </summary>
        [JsonProperty(PropertyName = "PatientID")]
        public string PatientId { get; set; }

        /// <summary>
        /// Gets or sets Access Number that is used to query the data source.
        /// Used when <c>Type</c> is <see cref="T:Monai.Deploy.InformaticsGateway.Api.Rest.InferenceRequestType.AccessionNumber" />.
        /// </summary>
        [JsonProperty(PropertyName = "accessionNumber")]
        public IList<string> AccessionNumber { get; set; }

        /// <summary>
        /// Gets or sets a list of FHIR resources to be retrived.
        /// </summary>
        [JsonProperty(PropertyName = "resources")]
        public IList<FhirResource> Resources { get; set; }

        /// <summary>
        /// Gets or set the data format used when storing FHIR resources.
        /// Defaults to JSON.
        /// </summary>
        [JsonProperty(PropertyName = "fhirFormat")]
        public FhirStorageFormat FhirFormat { get; set; } = FhirStorageFormat.Json;

        /// <summary>
        /// Gets or set the data format used when storing FHIR resources.
        /// Defaults to R3.
        /// </summary>
        [JsonProperty(PropertyName = "fhirVersion")]
        public FhirVersion FhirVersion { get; set; } = FhirVersion.R3;

        /// <summary>
        /// Gets the HTTP Accept Header used for sending a request.
        /// </summary>
        /// <value></value>
        public string FhirAcceptHeader
        {
            get
            {
                if (string.IsNullOrWhiteSpace(_fhirAcceptHeader))
                {
                    BuildFhirAcceptHeader();
                }
                return _fhirAcceptHeader;
            }
        }

        private void BuildFhirAcceptHeader()
        {
            var stringBuilder = new StringBuilder();

            switch (FhirFormat)
            {
                case FhirStorageFormat.Xml:
                    stringBuilder.Append("application/fhir+xml");
                    break;

                case FhirStorageFormat.Json:
                    stringBuilder.Append("application/fhir+json");
                    break;
            }
            stringBuilder.Append("; ");
            switch (FhirVersion)
            {
                case FhirVersion.R1:
                    stringBuilder.Append("fhirVersion=0.0");
                    break;

                case FhirVersion.R2:
                    stringBuilder.Append("fhirVersion=1.0");
                    break;

                case FhirVersion.R3:
                    stringBuilder.Append("fhirVersion=3.0");
                    break;

                case FhirVersion.R4:
                    stringBuilder.Append("fhirVersion=4.0");
                    break;
            }
            _fhirAcceptHeader = stringBuilder.ToString();
        }
    }
}
