using Messaging.Domain.Library.Messages;
using Messaging.Library.EventHubChannel;

namespace Messaging.RabbitMq.WebApi.Controllers;
public static class MessageHandler
{
    public static void Handle(CreateMessage message, IEventHub eventHub, ILogger<CreateMessage> logger)
    {
        // domain logic here
        logger.LogInformation("RabbitMq.MessageHandler Received Create Message: {@message}", message);
        eventHub.Publish("create-message", message);
        eventHub.Publish(message);

    }

    public static void Handle(InformationMessage message, ILogger<InformationMessage> logger)
    {
        // domain logic here
        logger.LogInformation("RabbitMq.MessageHandler Received Information Message: {@message}", message);
    }
}