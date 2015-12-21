using System.Linq;
using System.Threading.Tasks;
using Nimbus.Configuration.Settings;
using Nimbus.Infrastructure;
using Nimbus.Infrastructure.BrokeredMessageServices;
using Nimbus.Infrastructure.BrokeredMessageServices.Serialization;
using Nimbus.Infrastructure.Dispatching;
using Nimbus.Infrastructure.Events;
using Nimbus.Infrastructure.MessageSendersAndReceivers;
using Nimbus.Infrastructure.NimbusMessageServices;
using Nimbus.Infrastructure.Routing;
using Nimbus.MessageContracts;
using Nimbus.Tests.Common;
using Nimbus.Tests.Common.Stubs;
using Nimbus.UnitTests.BatchSendingTests.MessageContracts;
using NSubstitute;
using NUnit.Framework;
using Shouldly;

namespace Nimbus.UnitTests.BatchSendingTests
{
    [TestFixture]
    internal class WhenPublishingACollectionOfEventsViaTheBusEventSender : SpecificationForAsync<BusEventSender>
    {
        private INimbusMessageSender _nimbusMessageSender;

        protected override Task<BusEventSender> Given()
        {
            _nimbusMessageSender = Substitute.For<INimbusMessageSender>();

            var messagingFactory = Substitute.For<INimbusTransport>();
            messagingFactory.GetTopicSender(Arg.Any<string>()).Returns(ci => _nimbusMessageSender);

            var clock = new SystemClock();
            var typeProvider = new TestHarnessTypeProvider(new[] {GetType().Assembly}, new[] {GetType().Namespace});
            var serializer = new DataContractSerializer(typeProvider);
            var replyQueueNameSetting = new ReplyQueueNameSetting(
                new ApplicationNameSetting {Value = "TestApplication"},
                new InstanceNameSetting {Value = "TestInstance"});
            var brokeredMessageFactory = new NimbusMessageFactory(new DefaultMessageTimeToLiveSetting(),
                                                                    replyQueueNameSetting,
                                                                    clock,
                                                                    new DispatchContextManager());
            var logger = Substitute.For<ILogger>();
            var knownMessageTypeVerifier = Substitute.For<IKnownMessageTypeVerifier>();
            var router = new DestinationPerMessageTypeRouter();
            var dependencyResolver = new NullDependencyResolver();
            var outboundInterceptorFactory = new NullOutboundInterceptorFactory();
            var busCommandSender = new BusEventSender(dependencyResolver,
                                                      knownMessageTypeVerifier,
                                                      logger,
                                                      brokeredMessageFactory,
                                                      messagingFactory,
                                                      outboundInterceptorFactory, router);
            return Task.FromResult(busCommandSender);
        }

        protected override async Task When()
        {
            var events = new IBusEvent[] {new FooEvent(), new BarEvent(), new BazEvent()};

            foreach (var e in events)
            {
                await Subject.Publish(e);
            }
        }

        [Test]
        public void TheEventSenderShouldHaveReceivedThreeCalls()
        {
            _nimbusMessageSender.ReceivedCalls().Count().ShouldBe(3);
        }
    }
}