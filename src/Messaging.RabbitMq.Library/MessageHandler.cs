using Microsoft.Extensions.Logging;

namespace Messaging.RabbitMq.Library;

public class MessageHandler(ILogger<MessageHandler> logger)
{
    public void Handle(TextMessage message)
    {
        // domain logic here
        logger.LogInformation("Received Text Message: {@message}", message);
    }
    public void Handle(PingMessage message)
    {
        // domain logic here
        logger.LogInformation("Received Ping Message: {@message}", message);
    }

    public void Handle(HeartbeatMessage message)
    {
        // domain logic here
        logger.LogInformation("Received Heartbeat Message: {@message}", message);
    }

}