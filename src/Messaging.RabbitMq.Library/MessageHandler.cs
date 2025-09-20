using Messaging.Library;

using Microsoft.Extensions.Logging;

namespace Messaging.RabbitMq.Library;

public class MessageHandler(ISignalChannel channel, ILogger<MessageHandler> logger)
{
    public async Task Handle(TextMessage message, CancellationToken cancellationToken)
    {
        // domain logic here
        logger.LogInformation("Handler Received Text Message: {@message}", message);
        await channel.Send("TextMessage", message, cancellationToken);
        await channel.Send(message, cancellationToken);
        //await Task.Delay(1, cancellationToken);
    }
    public async Task Handle(PingMessage message, CancellationToken cancellationToken)
    {
        // domain logic here
        logger.LogInformation("Handler Received Ping Message: {@message}", message);
        await channel.Send("PingMessage", message, cancellationToken);
        await channel.Send(message, cancellationToken);
    }

    public async Task Handle(HeartbeatMessage message, CancellationToken cancellationToken)
    {
        // domain logic here
        logger.LogInformation("Handler Received Heartbeat Message: {@message}", message);
        await channel.Send("HeartbeatMessage", message, cancellationToken);
        await channel.Send(message, cancellationToken);
    }

}