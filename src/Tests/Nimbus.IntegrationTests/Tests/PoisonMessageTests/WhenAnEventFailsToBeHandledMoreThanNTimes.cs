﻿using System;
using System.Linq;
using System.Threading.Tasks;
using Nimbus.Configuration;
using Nimbus.IntegrationTests.Tests.PoisonMessageTests.MessageContracts;
using Nimbus.Tests.Common.Extensions;
using Nimbus.Tests.Common.TestScenarioGeneration.ScenarioComposition;
using Nimbus.Tests.Common.TestScenarioGeneration.TestCaseSources;
using Nimbus.Tests.Common.TestUtilities;
using NUnit.Framework;
using Shouldly;

namespace Nimbus.IntegrationTests.Tests.PoisonMessageTests
{
    [TestFixture]
    public class WhenAnEventFailsToBeHandledMoreThanNTimes : TestForBus
    {
        private GoBangEvent _goBangEvent;
        private string _someContent;
        private NimbusMessage[] _deadLetterMessages;

        private int _maxDeliveryAttempts;

        protected override async Task Given(IConfigurationScenario<BusBuilderConfiguration> scenario)
        {
            await base.Given(scenario);
            _maxDeliveryAttempts = Instance.Configuration.MaxDeliveryAttempts;
        }

        protected override async Task When()
        {
            _someContent = Guid.NewGuid().ToString();
            _goBangEvent = new GoBangEvent(_someContent);

            await Bus.Publish(_goBangEvent);
            await Timeout.WaitUntil(() => MethodCallCounter.AllReceivedCalls.Count() >= _maxDeliveryAttempts);

            _deadLetterMessages = await Bus.DeadLetterOffice.PopAll(1, TimeSpan.FromSeconds(TimeoutSeconds));
        }

        [Test]
        [TestCaseSource(typeof(AllBusConfigurations<WhenAnEventFailsToBeHandledMoreThanNTimes>))]
        public async Task Run(string testName, IConfigurationScenario<BusBuilderConfiguration> scenario)
        {
            await Given(scenario);
            await When();
            await Then();
        }

        [Then]
        public async Task ThereShouldBeExactlyOneMessageOnTheDeadLetterQueue()
        {
            _deadLetterMessages.Count().ShouldBe(1);
        }

        [Then]
        public async Task ThePayloadOfTheMessageShouldBeTheOriginalCommandThatWentBang()
        {
            ((GoBangEvent)_deadLetterMessages.Single().Payload).SomeContent.ShouldBe(_someContent);
        }

        [Then]
        public async Task TheMessageShouldHaveTheCorrectNumberOfDeliveryAttempts()
        {
            var nimbusMessage = _deadLetterMessages.Single();
            nimbusMessage.DeliveryAttempts.Count().ShouldBe(_maxDeliveryAttempts);
        }
    }
}
