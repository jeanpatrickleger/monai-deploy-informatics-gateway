﻿// SPDX-FileCopyrightText: © 2022 MONAI Consortium
// SPDX-FileCopyrightText: © 2019-2021 NVIDIA Corporation
// SPDX-License-Identifier: Apache License 2.0

using System.Collections.Generic;

namespace Monai.Deploy.InformaticsGateway.DicomWeb.Client.API
{
    /// <summary>
    /// IQidoService provides APIs to query studies, series and instances
    /// on a remote DICOMweb server.
    /// </summary>
    public interface IQidoService : IServiceBase
    {
        /// <summary>
        /// Search for all studies.
        /// </summary>
        IAsyncEnumerable<T> SearchForStudies<T>();

        /// <summary>
        /// Search for studies based on provided query parameters.
        /// </summary>
        /// <param name="queryParameters">A dictionary object where the <c>Key</c> contains the DICOM tag
        /// or keyword of an attribute and the <c>Value</c> contains the expected value to match.</param>
        IAsyncEnumerable<T> SearchForStudies<T>(IReadOnlyDictionary<string, string> queryParameters);

        /// <summary>
        /// Search for studies based on provided query parameters with additional DICOM fields to be included in the response message.
        /// </summary>
        /// <param name="queryParameters">A dictionary object where the <c>Key</c> contains the DICOM tag
        /// or keyword of an attribute and the <c>Value</c> contains the expected value to match.</param>
        /// <param name="fieldsToInclude">Liist of DICOM tags of name of the DICOM tag to be included in the response.</param>
        IAsyncEnumerable<T> SearchForStudies<T>(IReadOnlyDictionary<string, string> queryParameters, IReadOnlyList<string> fieldsToInclude);

        /// <summary>
        /// Search for studies based on provided query parameters with additional DICOM fields to be included in the response message.
        /// </summary>
        /// <param name="queryParameters">A dictionary object where the <c>Key</c> contains the DICOM tag
        /// or keyword of an attribute and the <c>Value</c> contains the expected value to match.</param>
        /// <param name="fieldsToInclude">Liist of DICOM tags of name of the DICOM tag to be included in the response.</param>
        /// <param name="fuzzyMatching">Whether fuzzy semantic matching should be performed.</param>
        IAsyncEnumerable<T> SearchForStudies<T>(IReadOnlyDictionary<string, string> queryParameters, IReadOnlyList<string> fieldsToInclude, bool fuzzyMatching);

        /// <summary>
        /// Search for studies based on provided query parameters with additional DICOM fields to be included in the response message.
        /// </summary>
        /// <param name="queryParameters">A dictionary object where the <c>Key</c> contains the DICOM tag
        /// or keyword of an attribute and the <c>Value</c> contains the expected value to match.</param>
        /// <param name="fieldsToInclude">Liist of DICOM tags of name of the DICOM tag to be included in the response.</param>
        /// <param name="limit">Maximum number of results to be returned.</param>
        IAsyncEnumerable<T> SearchForStudies<T>(IReadOnlyDictionary<string, string> queryParameters, IReadOnlyList<string> fieldsToInclude, bool fuzzyMatching, int limit);

        /// <summary>
        /// Search for studies based on provided query parameters with additional DICOM fields to be included in the response message.
        /// </summary>
        /// <param name="queryParameters">A dictionary object where the <c>Key</c> contains the DICOM tag
        /// or keyword of an attribute and the <c>Value</c> contains the expected value to match.</param>
        /// <param name="fieldsToInclude">Liist of DICOM tags of name of the DICOM tag to be included in the response.</param>
        /// <param name="limit">Maximum number of results to be returned.</param>
        /// <param name="offset">Number of results to be skipped.</param>
        IAsyncEnumerable<T> SearchForStudies<T>(IReadOnlyDictionary<string, string> queryParameters, IReadOnlyList<string> fieldsToInclude, bool fuzzyMatching, int limit, int offset);
    }
}
