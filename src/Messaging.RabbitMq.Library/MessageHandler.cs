using Messaging.Library;

using Microsoft.Extensions.Logging;

namespace Messaging.RabbitMq.Library;

public class MessageHandler(ISignalChannel channel, ILogger<MessageHandler> logger)
{
    public async Task Handle(TextMessage message, CancellationToken cancellationToken)
    {
        // domain logic here
        await channel.Publish("TextMessage", message, cancellationToken);
        await channel.Publish(message, cancellationToken);
        logger.LogInformation("Handler Received Text Message: {@message}", message);
        //await Task.Delay(1, cancellationToken);
    }
    public async Task Handle(PingMessage message, CancellationToken cancellationToken)
    {
        // domain logic here
        await channel.Publish("PingMessage", message, cancellationToken);
        await channel.Publish(message, cancellationToken);
        logger.LogInformation("Handler Received Ping Message: {@message}", message);
    }

    public async Task Handle(HeartbeatMessage message, CancellationToken cancellationToken)
    {
        // domain logic here
        await channel.Publish("HeartbeatMessage", message, cancellationToken);
        await channel.Publish(message, cancellationToken);
        logger.LogInformation("Handler Received Heartbeat Message: {@message}", message);
    }

}