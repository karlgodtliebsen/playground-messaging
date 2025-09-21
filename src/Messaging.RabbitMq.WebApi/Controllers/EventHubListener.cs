using Messaging.Domain.Library.Messages;
using Messaging.Library.EventHubChannel;

namespace Messaging.RabbitMq.WebApi.Controllers;

public class EventHubListener(IEventHub eventHub, ILogger<EventHubListener> logger)
{
    public void SetupSubscriptions()
    {
        eventHub.Subscribe<CreateMessage>("create-message", (createMessage, ct) =>
        {
            logger.LogInformation("EventListener using 'CreateMessage' Received CreateMessage: {@message}", createMessage);
            return Task.CompletedTask;
        });
        eventHub.Subscribe<CreateMessage>((createMessage, ct) =>
        {
            logger.LogInformation("EventListener Received CreateMessage: {@message}", createMessage);
            return Task.CompletedTask;
        });
        eventHub.SubscribeToAll((eventName, ct) =>
        {
            logger.LogInformation("EventListener Received Event: {eventName}", eventName);
            return Task.CompletedTask;
        });
    }



}