using Messaging.Library.EventHubChannel;

using Microsoft.Extensions.Logging;

namespace Messaging.Domain.Library.Messages.Handler;
public static class MessageHandler
{
    public static void Handle(CreateMessage message, IEventHub eventHub, ILogger<CreateMessage> logger)
    {
        // domain logic here
        logger.LogInformation("MessageHandler Received Create Message: {@message}", message);
        eventHub.Publish("create-message", message);
        eventHub.Publish(message);

    }

    public static void Handle(InformationMessage message, ILogger<InformationMessage> logger)
    {
        // domain logic here
        logger.LogInformation("MessageHandler Received Information Message: {@message}", message);
    }
}