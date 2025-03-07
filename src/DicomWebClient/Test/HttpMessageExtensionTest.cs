﻿// SPDX-FileCopyrightText: © 2021-2022 MONAI Consortium
// SPDX-FileCopyrightText: © 2019-2020 NVIDIA Corporation
// SPDX-License-Identifier: Apache License 2.0

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using FellowOakDicom;
using Monai.Deploy.InformaticsGateway.DicomWeb.Client.API;
using Monai.Deploy.InformaticsGateway.DicomWeb.Client.Common;
using Xunit;

namespace Monai.Deploy.InformaticsGateway.DicomWebClient.Test
{
    public class HttpMessageExtensionsTest
    {
        private const string PatientName = "DOE^JOHN";

        private readonly byte[] _byteData = new byte[]
        {
           0x01, 0x02, 0x03, 0x04, 0x05
        };

        #region AddRange Test

        [Fact(DisplayName = "AddRange shall throw when input is null")]
        public void AddRange_Null()
        {
            HttpRequestMessage request = null;
            Assert.Throws<ArgumentNullException>(() => request.AddRange(null));
        }

        [Fact(DisplayName = "AddRange when byteRange is null")]
        public void AddRange_ByteRangeIsNull()
        {
            HttpRequestMessage request = new HttpRequestMessage();
            request.AddRange(null);

            var range = request.Headers.Range;

            Assert.Equal("byte", range.Unit);
            Assert.Equal(0, range.Ranges.First().From);
            Assert.Null(range.Ranges.First().To);
        }

        [Fact(DisplayName = "AddRange when byteRange contains only start")]
        public void AddRange_ByteRangeHasOnlyStart()
        {
            HttpRequestMessage request = new HttpRequestMessage();
            request.AddRange(new Tuple<int, int?>(100, null));

            var range = request.Headers.Range;

            Assert.Equal("byte", range.Unit);
            Assert.Equal(100, range.Ranges.First().From);
            Assert.Null(range.Ranges.First().To);
        }

        [Fact(DisplayName = "AddRange when byteRange contains valid range")]
        public void AddRange_ByteRangeHasValidValues()
        {
            HttpRequestMessage request = new HttpRequestMessage();
            request.AddRange(new Tuple<int, int?>(100, 200));

            var range = request.Headers.Range;

            Assert.Equal("byte", range.Unit);
            Assert.Equal(100, range.Ranges.First().From);
            Assert.Equal(200, range.Ranges.First().To);
        }

        #endregion AddRange Test

        #region ToDicomAsyncEnumerable Test

        [Fact(DisplayName = "ToDicomAsyncEnumerable shall throw when input is null")]
        public async Task ToDicomAsyncEnumerable_Null()
        {
            HttpResponseMessage message = null;
            await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            {
                await foreach (var item in message.ToDicomAsyncEnumerable())
                { }
            });
        }

        [Fact(DisplayName = "ToDicomAsyncEnumerable shall throw with non-supported MIME type")]
        public async Task ToDicomAsyncEnumerable_NotMultipartRelated()
        {
            HttpResponseMessage message = new HttpResponseMessage();
            var multipartContent = new MultipartContent("form");
            await AddDicomFileContent(multipartContent);
            message.Content = multipartContent;
            await Assert.ThrowsAsync<ResponseDecodeException>(async () =>
            {
                await foreach (var item in message.ToDicomAsyncEnumerable())
                {
                }
            });
        }

        [Fact(DisplayName = "ToDicomAsyncEnumerable shall return DicomFiles")]
        public async Task ToDicomAsyncEnumerable_Ok()
        {
            HttpResponseMessage message = new HttpResponseMessage();
            var multipartContent = new MultipartContent("related");
            await AddDicomFileContent(multipartContent);
            message.Content = multipartContent;

            var result = new List<DicomFile>();
            await foreach (var item in message.ToDicomAsyncEnumerable())
            {
                result.Add(item);
            }

            Assert.Single(result);
            Assert.Equal(PatientName, result.First().Dataset.GetString(DicomTag.PatientName));
        }

        private static async Task AddDicomFileContent(MultipartContent multipartContent)
        {
            var dicomDataset = new DicomDataset(DicomTransferSyntax.ExplicitVRLittleEndian)
            {
                { DicomTag.PatientName, PatientName },
                { DicomTag.SOPClassUID, DicomUID.SecondaryCaptureImageStorage },
                { DicomTag.SOPInstanceUID, DicomUID.Generate() }
            };
            var dicomFile = new DicomFile(dicomDataset);

            using (var ms = new MemoryStream())
            {
                await dicomFile.SaveAsync(ms);
                multipartContent.Add(new ByteArrayContent(ms.ToArray()));
            }
        }

        #endregion ToDicomAsyncEnumerable Test

        #region ToBinaryData Test

        [Fact(DisplayName = "ToBinaryData shall throw when input is null")]
        public async Task ToBinaryData_Null()
        {
            HttpResponseMessage message = null;
            await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            {
                await message.ToBinaryData();
            });
        }

        [Fact(DisplayName = "ToDicomAsyncEnumerable shall return byte array")]
        public async Task ToBinaryData_Ok()
        {
            HttpResponseMessage message = new HttpResponseMessage();
            var multipartContent = new MultipartContent("related");
            AddByteArrayContent(multipartContent);
            message.Content = multipartContent;

            var result = await message.ToBinaryData().ConfigureAwait(false);

            Assert.Equal(_byteData, result);
        }

        private void AddByteArrayContent(MultipartContent multipartContent)
        {
            multipartContent.Add(new ByteArrayContent(_byteData));
        }

        #endregion ToBinaryData Test
    }
}
