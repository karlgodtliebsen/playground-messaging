using Messaging.Domain.Library.SimpleMessages;
using Microsoft.Extensions.Logging;

namespace Messaging.Kafka.Library.Services;
public static class MessageHandler
{
    public static void Handle(CreateMessage message, ILogger<CreateMessage> logger)
    {
        // domain logic here
        logger.LogInformation("MessageHandler Received Create Message: {@message}", message);
    }

    public static void Handle(InformationMessage message, ILogger<InformationMessage> logger)
    {
        // domain logic here
        logger.LogInformation("MessageHandler Received Information Message: {@message}", message);
    }
}