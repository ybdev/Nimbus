﻿using System;
using System.Threading.Tasks;
using Nimbus.Configuration;
using Nimbus.MessageContracts.Exceptions;
using Nimbus.Tests.Common;
using Nimbus.Tests.Common.Stubs;
using Nimbus.Transports.WindowsServiceBus;
using NUnit.Framework;
using Shouldly;

namespace Nimbus.IntegrationTests.Tests.BusBuilderTests
{
    [TestFixture]
    public class WhenStartingABusWithAnEndpointThatDoesNotExist
    {
        [Test]
        [Timeout(15*1000)]
        public async Task ItShouldGoBangQuickly()
        {
            var typeProvider = new TestHarnessTypeProvider(new[] {GetType().Assembly}, new[] {GetType().Namespace});

            var logger = TestHarnessLoggerFactory.Create();

            var bus = new BusBuilder().Configure()
                                      .WithTransport(new WindowsServiceBusTransportConfiguration()
                                                         .WithConnectionString(
                                                             @"Endpoint=sb://shouldnotexist.example.com/;SharedAccessKeyName=IntegrationTestHarness;SharedAccessKey=borkborkbork=")
                )
                                      .WithNames("IntegrationTestHarness", Environment.MachineName)
                                      .WithTypesFrom(typeProvider)
                                      .WithDefaultTimeout(TimeSpan.FromSeconds(10))
                                      .WithLogger(logger)
                                      .Build();

            Should.Throw<BusException>(() => bus.Start().Wait());
        }
    }
}