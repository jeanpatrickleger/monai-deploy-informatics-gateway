﻿// SPDX-FileCopyrightText: © 2021 - 2022 MONAI Consortium
// SPDX-FileCopyrightText: © 2019-2021 NVIDIA Corporation
//SPDX-License-Identifier: Apache License 2.0

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Monai.Deploy.InformaticsGateway.Api
{
    /// <summary>
    /// MONAI Application Entity
    /// MONAI's SCP AE Title is used to accept incoming associations and can, optionally, map to multiple workflows.
    /// </summary>
    /// <example>
    /// <code>
    /// {
    ///     "name": "brain-tumor",
    ///     "aeTitle": "BrainTumorModel"
    /// }
    /// </code>
    /// <code>
    /// {
    ///     "name": "COVID-19",
    ///     "aeTitle": "COVID-19",
    ///     "workflows": [ "EXAM", "Delta", "b75cd27a-068a-4f9c-b3da-e5d4ea08c55a"],
    ///     "grouping": [ "0010,0020"],
    ///     "ignoredSopClasses": ["1.2.840.10008.5.1.4.1.1.1.1"],
    ///     "timeout": 300
    /// }
    /// </code>
    /// </example>
    public class MonaiApplicationEntity
    {
        /// <summary>
        /// Gets or sets the name of a MONAI DICOM application entity.
        /// This value must be unique.
        /// </summary>
        [Key, Column(Order = 0)]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the AE TItle.
        /// </summary>
        public string AeTitle { get; set; }

        /// <summary>
        /// Gets or sets the DICOM tag used to group the instances.
        /// Defaults to 0020,000D (Study Instance UID).
        /// Valid DICOM Tags: > Study Instance UID (0020,000D) and Series Instance UID (0020,000E).
        /// </summary>
        public string Grouping { get; set; } = "0020,000D";

        /// <summary>
        /// Optional field to map AE to one or more workflows.
        /// </summary>
        public List<string> Workflows { get; set; }

        /// <summary>
        /// Optional field to specify SOP Class UIDs to ignore.
        /// </summary>
        public List<string> IgnoredSopClasses { get; set; }

        /// <summary>
        /// Timeout, in seconds, to wait for instances before notifying other subsystems of data arrival
        /// for the specified data group.
        /// Defaults to five seconds.
        /// </summary>
        public uint Timeout { get; set; } = 5;

        public MonaiApplicationEntity()
        {
            SetDefaultValues();
        }

        public void SetDefaultValues()
        {
            if (string.IsNullOrWhiteSpace(Name))
            {
                Name = AeTitle;
            }

            if (Workflows is null)
            {
                Workflows = new List<string>();
            }

            if (IgnoredSopClasses is null)
            {
                IgnoredSopClasses = new List<string>();
            }
        }
    }
}
