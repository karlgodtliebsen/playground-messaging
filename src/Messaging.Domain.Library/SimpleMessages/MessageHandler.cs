using Messaging.EventHub.Library;

using Microsoft.Extensions.Logging;

namespace Messaging.Domain.Library.SimpleMessages;

public class MessageHandler(IEventHub channel, ILogger<MessageHandler> logger)
{
    public async Task Handle(CreateMessage message, CancellationToken cancellationToken)
    {
        // domain logic here
        logger.LogInformation("SimpleMessages-MessageHandler Received Create Message: {@message}", message);
        await channel.Publish("CreateMessage", message, cancellationToken);
        await channel.Publish(message, cancellationToken);
    }
    public async Task Handle(InformationMessage message, CancellationToken cancellationToken)
    {
        // domain logic here
        logger.LogInformation("SimpleMessages-MessageHandler Received Information Message: {@message}", message);
        await channel.Publish("InformationMessage", message, cancellationToken);
        await channel.Publish(message, cancellationToken);
    }


}

