using Messaging.Library.Messages;
using Messaging.Library.Orders;
using Microsoft.Extensions.Logging;
using Wolverine;

namespace Messaging.RabbitMq.Library;

public class MessageHandler( /*IMessageBus messageBus,*/ ILogger<MessageHandler> logger)
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
}
