﻿// SPDX-FileCopyrightText: © 2021-2022 MONAI Consortium
// SPDX-License-Identifier: Apache License 2.0

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Monai.Deploy.InformaticsGateway.Api.Storage;
using Monai.Deploy.InformaticsGateway.Configuration;
using Monai.Deploy.InformaticsGateway.Repositories;
using Monai.Deploy.InformaticsGateway.Services.Connectors;
using Monai.Deploy.InformaticsGateway.SharedTest;
using Moq;
using xRetry;
using Xunit;

namespace Monai.Deploy.InformaticsGateway.Test.Services.Connectors
{
    public class PayloadAssemblerTest
    {
        private readonly Mock<IInformaticsGatewayRepository<Payload>> _repository;
        private readonly IOptions<InformaticsGatewayConfiguration> _options;
        private readonly Mock<ILogger<PayloadAssembler>> _logger;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly Mock<IServiceScopeFactory> _serviceScopeFactory;

        public PayloadAssemblerTest()
        {
            _serviceScopeFactory = new Mock<IServiceScopeFactory>();
            _repository = new Mock<IInformaticsGatewayRepository<Payload>>();
            _options = Options.Create(new InformaticsGatewayConfiguration());
            _logger = new Mock<ILogger<PayloadAssembler>>();
            _cancellationTokenSource = new CancellationTokenSource();

            var serviceProvider = new Mock<IServiceProvider>();
            serviceProvider
                .Setup(x => x.GetService(typeof(IInformaticsGatewayRepository<Payload>)))
                .Returns(_repository.Object);

            var scope = new Mock<IServiceScope>();
            scope.Setup(x => x.ServiceProvider).Returns(serviceProvider.Object);

            _serviceScopeFactory.Setup(p => p.CreateScope()).Returns(scope.Object);

            _options.Value.Storage.Retries.DelaysMilliseconds = new[] { 1, 1, 1 };
            _logger.Setup(p => p.IsEnabled(It.IsAny<LogLevel>())).Returns(true);
        }

        [Fact(DisplayName = "PayloadAssembler_Constructor")]
        public void PayloadAssembler_Constructor()
        {
            Assert.Throws<ArgumentNullException>(() => new PayloadAssembler(null, null, null));
            Assert.Throws<ArgumentNullException>(() => new PayloadAssembler(_options, null, null));
            Assert.Throws<ArgumentNullException>(() => new PayloadAssembler(_options, _logger.Object, null));
        }

        [RetryFact(DisplayName = "PayloadAssembler shall queue items using default timeout")]
        public async Task PayloadAssembler_ShallQueueWithDefaultTimeout()
        {
            var payloadAssembler = new PayloadAssembler(_options, _logger.Object, _serviceScopeFactory.Object);

            _ = Assert.ThrowsAsync<OperationCanceledException>(async () => await Task.Run(() => payloadAssembler.Dequeue(_cancellationTokenSource.Token)));

            await payloadAssembler.Queue("A", new TestStorageInfo("path"));

            _logger.VerifyLogging($"Bucket A created with timeout {PayloadAssembler.DEFAULT_TIMEOUT}s.", LogLevel.Information, Times.Once());
            payloadAssembler.Dispose();
            _cancellationTokenSource.Cancel();
        }

        [RetryFact(DisplayName = "PayloadAssembler shall restore from database on startup")]
        public async Task PayloadAssembler_ShallRestoreFromDatabase()
        {
            var dataset = new List<Payload>
            {
                new Payload("created-test", Guid.NewGuid().ToString(), 10) { State = Payload.PayloadState.Created },
                new Payload("upload-test", Guid.NewGuid().ToString(), 10) { State = Payload.PayloadState.Upload },
                new Payload("notify-test", Guid.NewGuid().ToString(), 10) { State = Payload.PayloadState.Notify },
            };
            _repository.Setup(p => p.AsQueryable()).Returns(dataset.AsQueryable());
            var payloadAssembler = new PayloadAssembler(_options, _logger.Object, _serviceScopeFactory.Object);
            await Task.Delay(250);
            payloadAssembler.Dispose();
            _cancellationTokenSource.Cancel();

            _logger.VerifyLogging($"Restoring payloads from database.", LogLevel.Information, Times.Once());
            _logger.VerifyLogging($"1 payloads restored from database.", LogLevel.Information, Times.Once());
        }

        [RetryFact(DisplayName = "PayloadAssembler shall retry when it fails to add payload to database")]
        public async Task PayloadAssembler_ShallRetryUponAddDatabaseFailure()
        {
            _repository.Setup(p => p.SaveChangesAsync(It.IsAny<CancellationToken>())).Throws(new Exception("error"));

            var payloadAssembler = new PayloadAssembler(_options, _logger.Object, _serviceScopeFactory.Object);

            _ = Assert.ThrowsAsync<OperationCanceledException>(async () => await Task.Run(() => payloadAssembler.Dequeue(_cancellationTokenSource.Token)));

            await Assert.ThrowsAsync<Exception>(async () => await payloadAssembler.Queue("A", new TestStorageInfo("path")));

            _logger.VerifyLogging($"Bucket A created with timeout {PayloadAssembler.DEFAULT_TIMEOUT}s.", LogLevel.Information, Times.Never());
            payloadAssembler.Dispose();
            _cancellationTokenSource.Cancel();
        }

        [RetryFact(DisplayName = "PayloadAssembler shall be disposed properly")]
        public async Task PayloadAssembler_ShallBeDisposedProperly()
        {
            var payloadAssembler = new PayloadAssembler(_options, _logger.Object, _serviceScopeFactory.Object);

            _ = Assert.ThrowsAsync<OperationCanceledException>(async () => await Task.Run(() => payloadAssembler.Dequeue(_cancellationTokenSource.Token)));

            await payloadAssembler.Queue("A", new TestStorageInfo("path"));

            payloadAssembler.Dispose();
            _cancellationTokenSource.Cancel();

            await Task.Delay(1000);
            _logger.VerifyLoggingMessageBeginsWith($"Number of collections in queue", LogLevel.Trace, Times.Never());
        }

        [RetryFact(DisplayName = "PayloadAssembler shall retry saving payload to database on timed event")]
        public async Task PayloadAssembler_ShallRetryUponSaveToDatabaseFailure()
        {
            int callCount = 0;
            _repository.Setup(p => p.SaveChangesAsync(It.IsAny<CancellationToken>())).Callback(() =>
            {
                if (callCount == _options.Value.Storage.Retries.DelaysMilliseconds.Length + 1)
                {
                    _cancellationTokenSource.Cancel();
                }
                if (callCount++ != 0)
                {
                    throw new Exception("error");
                }
            });

            var payloadAssembler = new PayloadAssembler(_options, _logger.Object, _serviceScopeFactory.Object);

            await payloadAssembler.Queue("A", new TestStorageInfo("path"), 1);
            _cancellationTokenSource.Token.WaitHandle.WaitOne();
            payloadAssembler.Dispose();

            _logger.VerifyLoggingMessageBeginsWith($"Number of buckets active: 1.", LogLevel.Trace, Times.AtLeastOnce());
            _logger.VerifyLoggingMessageBeginsWith($"Error processing bucket", LogLevel.Warning, Times.AtLeastOnce());
        }

        [RetryFact(DisplayName = "PayloadAssembler shall enqueue payload on timed event")]
        public async Task PayloadAssembler_ShallEnqueuePayloadOnTimedEvent()
        {
            var payloadAssembler = new PayloadAssembler(_options, _logger.Object, _serviceScopeFactory.Object);

            await payloadAssembler.Queue("A", new TestStorageInfo("path"), 1);
            await Task.Delay(1001);
            var result = payloadAssembler.Dequeue(_cancellationTokenSource.Token);
            payloadAssembler.Dispose();

            _logger.VerifyLoggingMessageBeginsWith($"Number of buckets active: 1.", LogLevel.Trace, Times.AtLeastOnce());
            Assert.Single(result.Files);
            _logger.VerifyLoggingMessageBeginsWith($"Bucket A sent to processing queue with {result.Count} files", LogLevel.Information, Times.AtLeastOnce());
        }
    }
}
