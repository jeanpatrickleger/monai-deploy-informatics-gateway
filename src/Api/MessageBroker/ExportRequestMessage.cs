﻿// Copyright 2021-2022 MONAI Consortium
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//     http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System.Collections.Generic;
using System.Linq;

namespace Monai.Deploy.InformaticsGateway.Api.MessageBroker
{
    public class ExportRequestMessage
    {
        /// <summary>
        /// Gets or sets the workflow ID generated by the Workflow Manager.
        /// </summary>
        public string WorkflowId { get; set; }

        /// <summary>
        /// Gets or sets the export task ID generated by the Workflow Manager.
        /// </summary>
        public string ExportTaskId { get; set; }

        /// <summary>
        /// Gets or sets a list of files to be exported.
        /// </summary>
        public IEnumerable<string> Files { get; set; }

        /// <summary>
        /// Gets or sets the export target.
        /// For DIMSE, the named DICOM destination.
        /// For ACR, the Transaction ID in the original inference request.
        /// </summary>
        public string Destination { get; set; }

        /// <summary>
        /// Gets or set the correation ID.
        /// For DIMSE, the correlation ID is the UUID associated with the first DICOM association received.
        /// For ACR, use the Transaction ID in the original request.
        /// </summary>
        public string CorrelationId { get; set; }

        /// <summary>
        /// Gets or set number of files exported successfully.
        /// </summary>
        public int SucceededFiles { get; set; } = 0;

        /// <summary>
        /// Gets or sets number of files failed to export.
        /// </summary>
        public int FailedFiles { get; set; } = 0;

        /// <summary>
        /// Gets or sets the delivery tag or acknowledge token for the task.
        /// </summary>
        public string DeliveryTag { get; set; }

        /// <summary>
        /// Gets or sets the message ID set by the message broker.
        /// </summary>
        public string MessageId { get; set; }

        /// <summary>
        /// Gets wether the export task is completed or not based on file count.
        /// </summary>
        public bool IsCompleted
        { get { return (SucceededFiles + FailedFiles) == Files.Count(); } }

        /// <summary>
        /// Gets or sets error messages related to this export task.
        /// </summary>
        public List<string> ErrorMessages { get; init; }

        public ExportRequestMessage()
        {
            ErrorMessages = new List<string>();
        }

        public void AddErrorMessages(IList<string> errorMessages)
        {
            ErrorMessages.AddRange(errorMessages);
        }
    }
}