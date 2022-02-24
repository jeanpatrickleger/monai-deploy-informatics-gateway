// Copyright 2022 MONAI Consortium
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//     http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Net;
using System.Threading.Tasks;
using Ardalis.GuardClauses;
using FellowOakDicom;
using FellowOakDicom.Network;
using Minio;
using Monai.Deploy.InformaticsGateway.Api;
using Monai.Deploy.InformaticsGateway.Api.MessageBroker;
using Monai.Deploy.InformaticsGateway.Client;
using Monai.Deploy.InformaticsGateway.Client.Common;
using Monai.Deploy.InformaticsGateway.Integration.Test.Common;
using Monai.Deploy.InformaticsGateway.Integration.Test.Drivers;
using Monai.Deploy.InformaticsGateway.Integration.Test.Hooks;
using TechTalk.SpecFlow;
using TechTalk.SpecFlow.Infrastructure;

namespace Monai.Deploy.InformaticsGateway.Integration.Test.StepDefinitions
{
    [Binding]
    [CollectionDefinition("SpecFlowNonParallelizableFeatures", DisableParallelization = true)]
    public class DicomDimseScuServicesStepDefinitions
    {
        internal static readonly TimeSpan DicomScpWaitTimeSpan = TimeSpan.FromMinutes(2);
        internal static readonly string KeyDicomHashes = "DICOM_FILES";
        internal static readonly string KeyCalledAet = "CALLED_AET";
        internal static readonly string KeyExportRequestMessage = "EXPORT_REQUEST-MESSAGE";
        private readonly FeatureContext _featureContext;
        private readonly ScenarioContext _scenarioContext;
        private readonly ISpecFlowOutputHelper _outputHelper;
        private readonly Configurations _configuration;
        private readonly DicomInstanceGenerator _dicomInstanceGenerator;
        private readonly InformaticsGatewayClient _informaticsGatewayClient;
        private readonly RabbitMqHooks _rabbitMqHooks;

        public DicomDimseScuServicesStepDefinitions(
            FeatureContext featureContext,
            ScenarioContext scenarioContext,
            ISpecFlowOutputHelper outputHelper,
            Configurations configuration,
            DicomInstanceGenerator dicomInstanceGenerator,
            InformaticsGatewayClient informaticsGatewayClient,
            RabbitMqHooks rabbitMqHooks)
        {
            _featureContext = featureContext ?? throw new ArgumentNullException(nameof(featureContext));
            _scenarioContext = scenarioContext ?? throw new ArgumentNullException(nameof(scenarioContext));
            _outputHelper = outputHelper ?? throw new ArgumentNullException(nameof(outputHelper));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _dicomInstanceGenerator = dicomInstanceGenerator ?? throw new ArgumentNullException(nameof(dicomInstanceGenerator));
            _informaticsGatewayClient = informaticsGatewayClient ?? throw new ArgumentNullException(nameof(informaticsGatewayClient));
            _rabbitMqHooks = rabbitMqHooks ?? throw new ArgumentNullException(nameof(rabbitMqHooks));
            _informaticsGatewayClient.ConfigureServiceUris(new Uri(_configuration.InformaticsGatewayOptions.ApiEndpoint));
        }

        [Given(@"a DICOM destination registered with Informatics Gateway")]
        public async Task GivenADicomScpWithAET()
        {
            DestinationApplicationEntity destination;
            try
            {
                _scenarioContext[KeyCalledAet] = destination = await _informaticsGatewayClient.DicomDestinations.Create(new DestinationApplicationEntity
                {
                    Name = ScpHooks.FeatureScpAeTitle,
                    AeTitle = ScpHooks.FeatureScpAeTitle,
                    HostIp = _configuration.TestRunnerOptions.HostIp,
                    Port = ScpHooks.FeatureScpPort
                }, CancellationToken.None);
            }
            catch (ProblemException ex)
            {
                if (ex.ProblemDetails.Status == (int)HttpStatusCode.BadRequest && ex.ProblemDetails.Detail.Contains("already exists"))
                {
                    _scenarioContext[KeyCalledAet] = destination =
                        await _informaticsGatewayClient.DicomDestinations.Get(ScpHooks.FeatureScpAeTitle, CancellationToken.None);
                }
                else
                {
                    throw;
                }
            }
        }

        [Given(@"(.*) (.*) studies for export")]
        public async Task GivenDICOMInstances(int studyCount, string modality)
        {
            Guard.Against.NegativeOrZero(studyCount, nameof(studyCount));
            Guard.Against.NullOrWhiteSpace(modality, nameof(modality));

            _outputHelper.WriteLine($"Generating {studyCount} {modality} study");
            _configuration.StudySpecs.ContainsKey(modality).Should().BeTrue();

            var studySpec = _configuration.StudySpecs[modality];
            var patientId = DateTime.Now.ToString("yyyyMMddHHmmssfff");
            var fileSpecs = _dicomInstanceGenerator.Generate(patientId, studyCount, modality, studySpec);

            var hashes = new Dictionary<string, string>();

            _outputHelper.WriteLine($"File specs: {fileSpecs.StudyCount} studies, {fileSpecs.SeriesPerStudyCount} series/study, {fileSpecs.InstancePerSeries} instances/series, {fileSpecs.FileCount} files total");

            var minioClient = new MinioClient(_configuration.StorageServiceOptions.Endpoint, _configuration.StorageServiceOptions.AccessKey, _configuration.StorageServiceOptions.AccessToken);

            _outputHelper.WriteLine($"Uploading {fileSpecs.FileCount} files to MinIO...");
            foreach (var file in fileSpecs.Files)
            {
                var filename = file.GenerateFileName();
                hashes.Add(filename, file.CalculateHash());

                var stream = new MemoryStream();
                await file.SaveAsync(stream);
                stream.Position = 0;
                await minioClient.PutObjectAsync(_configuration.TestRunnerOptions.Bucket, filename, stream, stream.Length);
            }
            _scenarioContext[KeyDicomHashes] = hashes;
        }

        [When(@"a export request is sent for '([^']*)'")]
        public void WhenAExportRequestIsReceivedDesignatedFor(string routingKey)
        {
            Guard.Against.NullOrWhiteSpace(routingKey, nameof(routingKey));

            var dicomHashes = _scenarioContext[KeyDicomHashes] as Dictionary<string, string>;
            var calledAet = _scenarioContext[KeyCalledAet] as DestinationApplicationEntity;

            var exportRequestMessage = new ExportRequestMessage
            {
                CorrelationId = Guid.NewGuid().ToString(),
                Destination = calledAet.Name,
                ExportTaskId = Guid.NewGuid().ToString(),
                Files = dicomHashes.Keys.ToList(),
                MessageId = Guid.NewGuid().ToString(),
                WorkflowId = Guid.NewGuid().ToString(),
            };

            var message = new JsonMessage<ExportRequestMessage>(
                exportRequestMessage,
                exportRequestMessage.CorrelationId,
                string.Empty);

            _rabbitMqHooks.SetupMessageHandle(1);
            _rabbitMqHooks.Publish(routingKey, message.ToMessage());
            _scenarioContext[KeyExportRequestMessage] = exportRequestMessage;
        }


        [Then(@"Informatics Gateway exports the studies to the DICOM SCP")]
        public async Task ThenExportTheInstancesToTheDicomScp()
        {
            _rabbitMqHooks.MessageWaitHandle.Wait(DicomScpWaitTimeSpan).Should().BeTrue();
            var data = _featureContext[ScpHooks.KeyServerData] as ServerData;
            var dicomHashes = _scenarioContext[KeyDicomHashes] as Dictionary<string, string>;

            foreach (var key in dicomHashes.Keys)
            {
                (await Extensions.WaitUntil(() => data.Instances.ContainsKey(key), DicomScpWaitTimeSpan)).Should().BeTrue("{0} should be received", key);
                data.Instances.Should().ContainKey(key).WhoseValue.Equals(dicomHashes[key]);
            }
        }
    }
}
