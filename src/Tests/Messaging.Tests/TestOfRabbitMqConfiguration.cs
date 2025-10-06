using FluentAssertions;

using Messaging.Domain.Library.DemoMessages;
using Messaging.RabbitMq.Library.LegacySupport;

namespace Messaging.Tests;

public class TestOfRabbitMqConfiguration
{
    [Fact]
    public void VerifyThatLegacyTypeMapperIsValid()
    {
        LegacyTypeMapper mapper = new LegacyTypeMapper();
        mapper.Register<TextMessage>(typeof(TextMessage).FullName!);
        mapper.Register<PingMessage>(typeof(PingMessage).FullName!);
        mapper.Register<HeartbeatMessage>(typeof(HeartbeatMessage).FullName!);

        var (fullName, assemblyName) = mapper.MapToLegacy<TextMessage>();
        fullName.Should().Be("Messaging.Domain.Library.DemoMessages.TextMessage");
        assemblyName.Should().Be("Messaging.Domain.Library");

        var typeName = mapper.MapFromLegacy(fullName, assemblyName);
        typeName.Should().Be("Messaging.Domain.Library.DemoMessages.TextMessage");
    }

    [Fact]
    public void VerifyThatQueueMapperIsValid()
    {
        //var messageQueueNameRegistration = new TypeToQueueRegistry();
        //messageQueueNameRegistration.Register<TextMessage>("text-message-queue");
        //messageQueueNameRegistration.Register<PingMessage>("diagnostics-queue");
        //messageQueueNameRegistration.Register<HeartbeatMessage>("diagnostics-queue");

        //messageQueueNameRegistration.TryLookup<TextMessage>().Should().Be("text-message-queue");
        //messageQueueNameRegistration.TryLookup<PingMessage>().Should().Be("diagnostics-queue");
        //messageQueueNameRegistration.TryLookup<HeartbeatMessage>().Should().Be("diagnostics-queue");


    }
    [Fact]
    public void VerifyThatProducerIsBuild()
    {
        //TBD
    }

    [Fact]
    public void VerifyThatConsumerIsBuild()
    {
        //TBD
    }
}