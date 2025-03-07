﻿// SPDX-FileCopyrightText: © 2021-2022 MONAI Consortium
// SPDX-License-Identifier: Apache License 2.0

using Microsoft.Extensions.Configuration;
using Monai.Deploy.Messaging.Configuration;

namespace Monai.Deploy.InformaticsGateway.Configuration
{
    public class MessageBrokerConfiguration : MessageBrokerServiceConfiguration
    {
        public static readonly string InformaticsGatewayApplicationId = "16988a78-87b5-4168-a5c3-2cfc2bab8e54";

        /// <summary>
        /// Gets or sets retry options relate to the message broker services.
        /// </summary>
        [ConfigurationKeyName("retries")]
        public RetryConfiguration Retries { get; set; } = new RetryConfiguration();

        /// <summary>
        /// Gets or sets the topics for events published/subscribed by Informatics Gateway
        /// </summary>
        [ConfigurationKeyName("topics")]
        public MessageBrokerConfigurationKeys Topics { get; set; } = new MessageBrokerConfigurationKeys();
    }
}
