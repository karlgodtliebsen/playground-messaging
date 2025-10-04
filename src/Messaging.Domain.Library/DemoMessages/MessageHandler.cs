using Messaging.EventHub.Library;

using Microsoft.Extensions.Logging;

namespace Messaging.Domain.Library.DemoMessages;

public class MessageHandler(IEventHub channel, ILogger<MessageHandler> logger)
{
    public async Task Handle(TextMessage message, CancellationToken cancellationToken)
    {
        // domain logic here
        logger.LogInformation("DemoMessages-MessageHandler Received Text Message: {@message}", message);
        await channel.Publish("TextMessage", message, cancellationToken);
        await channel.Publish(message, cancellationToken);
    }
    public async Task Handle(PingMessage message, CancellationToken cancellationToken)
    {
        // domain logic here
        logger.LogInformation("DemoMessages-MessageHandler Received Ping Message: {@message}", message);
        await channel.Publish("PingMessage", message, cancellationToken);
        await channel.Publish(message, cancellationToken);
    }

    public async Task Handle(HeartbeatMessage message, CancellationToken cancellationToken)
    {
        // domain logic here
        logger.LogInformation("DemoMessages-MessageHandler Received Heartbeat Message: {@message}", message);
        await channel.Publish("HeartbeatMessage", message, cancellationToken);
        await channel.Publish(message, cancellationToken);
    }
}