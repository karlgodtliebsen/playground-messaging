using Microsoft.Extensions.Logging;

namespace Messaging.Library.Messages.Handler;
public static class MessageHandler
{
    public static void Handle(CreateMessage message, ILogger<CreateMessage> logger)
    {
        // domain logic here
        logger.LogInformation("Received Create Message: {@message}", message);
    }

    public static void Handle(InformationMessage message, ILogger<InformationMessage> logger)
    {
        // domain logic here
        logger.LogInformation("Received Information Message: {@message}", message);
    }
}