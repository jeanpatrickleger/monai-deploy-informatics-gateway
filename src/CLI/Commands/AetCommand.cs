﻿// Copyright 2021 MONAI Consortium
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//     http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Ardalis.GuardClauses;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Monai.Deploy.InformaticsGateway.Api;
using Monai.Deploy.InformaticsGateway.CLI.Services;
using Monai.Deploy.InformaticsGateway.Client;
using Monai.Deploy.InformaticsGateway.Common;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.Rendering;
using System.CommandLine.Rendering.Views;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Monai.Deploy.InformaticsGateway.CLI
{
    public class AetCommand : CommandBase
    {
        public AetCommand() : base("aet", "Configure SCP Application Entities")
        {
            this.AddAlias("aetitle");

            SetupAddAetCommand();
            SetupRemoveAetCommand();
            SetupListAetCommand();
        }

        private void SetupListAetCommand()
        {
            var listCommand = new Command("ls", "List all SCP Application Entities");
            listCommand.AddAlias("list");
            this.AddCommand(listCommand);

            listCommand.Handler = CommandHandler.Create<IHost, bool, CancellationToken>(ListAeTitlehandlerAsync);
        }

        private void SetupRemoveAetCommand()
        {
            var removeCommand = new Command("rm", "Remove a SCP Application Entity");
            removeCommand.AddAlias("del");
            this.AddCommand(removeCommand);

            var nameOption = new Option<string>(new string[] { "-n", "--name" }, "Name of the SCP Application Entity") { IsRequired = true };
            removeCommand.AddOption(nameOption);

            removeCommand.Handler = CommandHandler.Create<string, IHost, bool, CancellationToken>(RemoveAeTitlehandlerAsync);
        }

        private void SetupAddAetCommand()
        {
            var addCommand = new Command("add", "Add a new SCP Application Entity");
            this.AddCommand(addCommand);

            var nameOption = new Option<string>(new string[] { "-n", "--name" }, "Name of the SCP Application Entity") { IsRequired = false };
            addCommand.AddOption(nameOption);
            var aeTitleOption = new Option<string>(new string[] { "-a", "--aetitle" }, "AE Title of the SCP") { IsRequired = true };
            addCommand.AddOption(aeTitleOption);
            var appsOption = new Option<string[]>(new string[] { "--apps" }, () => Array.Empty<string>(), "A space separated list of application names or IDs to be associated with the SCP AE Title")
            {
                AllowMultipleArgumentsPerToken = true,
                IsRequired = false,
                Name = "--applications"
            };
            addCommand.AddOption(appsOption);

            addCommand.Handler = CommandHandler.Create<MonaiApplicationEntity, IHost, bool, CancellationToken>(AddAeTitlehandlerAsync);
        }

        private async Task<int> ListAeTitlehandlerAsync(IHost host, bool verbose, CancellationToken cancellationToken)
        {
            this.LogVerbose(verbose, host, "Configuring services...");

            var console = host.Services.GetRequiredService<IConsole>();
            var config = host.Services.GetRequiredService<IConfigurationService>();
            var client = host.Services.GetRequiredService<IInformaticsGatewayClient>();
            var consoleRegion = host.Services.GetRequiredService<IConsoleRegion>();
            var logger = CreateLogger<AetCommand>(host);

            Guard.Against.Null(logger, nameof(logger), "Logger is unavailable.");
            Guard.Against.Null(console, nameof(console), "Console service is unavailable.");
            Guard.Against.Null(config, nameof(config), "Configuration service is unavailable.");
            Guard.Against.Null(client, nameof(client), $"{Strings.ApplicationName} client is unavailable.");
            Guard.Against.Null(consoleRegion, nameof(consoleRegion), "Console region is unavailable.");

            IReadOnlyList<MonaiApplicationEntity> items = null;
            try
            {
                client.ConfigureServiceUris(config.InformaticsGatewayServerUri);
                this.LogVerbose(verbose, host, $"Connecting to {Strings.ApplicationName} at {config.InformaticsGatewayServer}...");
                this.LogVerbose(verbose, host, $"Retrieving MONAI SCP AE Titles...");
                items = await client.MonaiScpAeTitle.List(cancellationToken);
            }
            catch (ConfigurationException ex)
            {
                logger.Log(LogLevel.Critical, ex.Message);
                return ExitCodes.Config_NotConfigured;
            }
            catch (Exception ex)
            {
                logger.Log(LogLevel.Critical, $"Error retrieving MONAI SCP AE Titles: {ex.Message}");
                return ExitCodes.MonaiScp_ErrorList;
            }

            if (items.IsNullOrEmpty())
            {
                logger.Log(LogLevel.Warning, "No MONAI SCP Application Entities configured.");
            }
            else
            {
                if (console is ITerminal terminal)
                {
                    terminal.Clear();
                }
                var consoleRenderer = new ConsoleRenderer(console);

                var table = new TableView<MonaiApplicationEntity>
                {
                    Items = items.OrderBy(p => p.Name).ToList()
                };
                table.AddColumn(p => p.Name, new ContentView("Name".Underline()));
                table.AddColumn(p => p.AeTitle, new ContentView("AE Title".Underline()));
                table.AddColumn(p => p.Applications.IsNullOrEmpty() ? "n/a" : string.Join(", ", p.Applications), new ContentView("Applications".Underline()));
                table.Render(consoleRenderer, consoleRegion.GetDefaultConsoleRegion());
            }
            return ExitCodes.Success;
        }

        private async Task<int> RemoveAeTitlehandlerAsync(string name, IHost host, bool verbose, CancellationToken cancellationToken)
        {
            this.LogVerbose(verbose, host, "Configuring services...");
            var config = host.Services.GetRequiredService<IConfigurationService>();
            var client = host.Services.GetRequiredService<IInformaticsGatewayClient>();
            var logger = CreateLogger<AetCommand>(host);

            Guard.Against.Null(logger, nameof(logger), "Logger is unavailable.");
            Guard.Against.Null(config, nameof(config), "Configuration service is unavailable.");
            Guard.Against.Null(client, nameof(client), $"{Strings.ApplicationName} client is unavailable.");

            try
            {
                client.ConfigureServiceUris(config.InformaticsGatewayServerUri);
                this.LogVerbose(verbose, host, $"Connecting to {Strings.ApplicationName} at {config.InformaticsGatewayServer}...");
                this.LogVerbose(verbose, host, $"Deleting MONAI SCP AE Title {name}...");
                _ = await client.MonaiScpAeTitle.Delete(name, cancellationToken);
                logger.Log(LogLevel.Information, $"MONAI SCP AE Title '{name}' deleted.");
            }
            catch (ConfigurationException ex)
            {
                logger.Log(LogLevel.Critical, ex.Message);
                return ExitCodes.Config_NotConfigured;
            }
            catch (Exception ex)
            {
                logger.Log(LogLevel.Critical, $"Error deleting MONAI SCP AE Title {name}: {ex.Message}");
                return ExitCodes.MonaiScp_ErrorDelete;
            }
            return ExitCodes.Success;
        }

        private async Task<int> AddAeTitlehandlerAsync(MonaiApplicationEntity entity, IHost host, bool verbose, CancellationToken cancellationToken)
        {
            this.LogVerbose(verbose, host, "Configuring services...");
            var config = host.Services.GetRequiredService<IConfigurationService>();
            var client = host.Services.GetRequiredService<IInformaticsGatewayClient>();
            var logger = CreateLogger<AetCommand>(host);

            Guard.Against.Null(logger, nameof(logger), "Logger is unavailable.");
            Guard.Against.Null(config, nameof(config), "Configuration service is unavailable.");
            Guard.Against.Null(client, nameof(client), $"{Strings.ApplicationName} client is unavailable.");

            try
            {
                client.ConfigureServiceUris(config.InformaticsGatewayServerUri);

                this.LogVerbose(verbose, host, $"Connecting to {Strings.ApplicationName} at {config.InformaticsGatewayServer}...");
                var result = await client.MonaiScpAeTitle.Create(entity, cancellationToken);

                logger.Log(LogLevel.Information, "New MONAI Deploy SCP Application Entity created:");
                logger.Log(LogLevel.Information, "\tName:     {0}", result.Name);
                logger.Log(LogLevel.Information, "\tAE Title: {0}", result.AeTitle);

                if (result.Applications.Any())
                {
                    logger.Log(LogLevel.Information, "\tApplications:{0}", string.Join(',', result.Applications));
                    logger.Log(LogLevel.Warning, "Data received by this Application Entity will bypass Data Routing Service.");
                }
            }
            catch (ConfigurationException ex)
            {
                logger.Log(LogLevel.Critical, ex.Message);
                return ExitCodes.Config_NotConfigured;
            }
            catch (Exception ex)
            {
                logger.Log(LogLevel.Critical, $"Error creating MONAI SCP AE Title {entity.AeTitle}: {ex.Message}");
                return ExitCodes.MonaiScp_ErrorCreate;
            }
            return ExitCodes.Success;
        }
    }
}
