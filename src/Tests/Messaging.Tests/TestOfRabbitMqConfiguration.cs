using FluentAssertions;

using Messaging.RabbitMq.Library;
using Messaging.RabbitMq.Library.Configuration;

using HeartbeatMessage = Org.BouncyCastle.Tls.HeartbeatMessage;

namespace Messaging.Tests;

public class TestOfRabbitMqConfiguration
{
    [Fact]
    public void VerifyThatLegacyTypeMapperIsValid()
    {
        LegacyTypeMapper mapper = new LegacyTypeMapper();
        mapper.Register<TextMessage>(typeof(TextMessage).FullName!);
        mapper.Register<PingMessage>(typeof(PingMessage).FullName!);
        mapper.Register<RabbitMq.Library.HeartbeatMessage>(typeof(RabbitMq.Library.HeartbeatMessage).FullName!);

        var (fullName, assemblyName) = mapper.MapToLegacy<TextMessage>();
        fullName.Should().Be("Messaging.RabbitMq.Library.HeartbeatMessage");
        assemblyName.Should().Be("Messaging.RabbitMq.Library");

        var typeName = mapper.MapFromLegacy(fullName, assemblyName);
        typeName.Should().Be("Messaging.RabbitMq.Library.HeartbeatMessage");
    }

    [Fact]
    public void VerifyThatQueueMapperIsValid()
    {
        var messageQueueNameRegistration = new TypeToQueueMapper();
        messageQueueNameRegistration.Register<TextMessage>("text-message-queue");
        messageQueueNameRegistration.Register<PingMessage>("diagnostics-queue");
        messageQueueNameRegistration.Register<HeartbeatMessage>("diagnostics-queue");

        messageQueueNameRegistration.TryLookup<TextMessage>().Should().Be("text-message-queue");
        messageQueueNameRegistration.TryLookup<PingMessage>().Should().Be("diagnostics-queue");
        messageQueueNameRegistration.TryLookup<HeartbeatMessage>().Should().Be("diagnostics-queue");


    }
    [Fact]
    public void VerifyThatProducerIsBuild()
    {

    }
}