using Messaging.Domain.Library.DemoMessages;
using Messaging.Library.EventHubChannel;
using Microsoft.Extensions.Logging;

namespace Messaging.Console.App.Configuration;

public class EventHubListener(IEventHub eventHub, ILogger<EventHubListener> logger)
{
    public void SetupSubscriptions()
    {
        eventHub.Subscribe("Alive", (ct) =>
        {
            logger.LogInformation("EventListener Received Alive Signal");
            return Task.CompletedTask;
        });

        eventHub.Subscribe<TextMessage>("TextMessage", (m, ct) =>
        {
            logger.LogInformation("EventListener using 'TextMessage' Received TextMessage: {@message}", m);
            return Task.CompletedTask;
        });

        eventHub.Subscribe<TextMessage>((m, ct) =>
        {
            logger.LogInformation("EventListener Received TextMessage: {@message}", m);
            return Task.CompletedTask;
        });

        eventHub.Subscribe<PingMessage>((m, ct) =>
        {
            logger.LogInformation("EventListener Received PingMessage: {@message}", m);
            return Task.CompletedTask;
        });
        eventHub.Subscribe<HeartbeatMessage>((m, ct) =>
        {
            logger.LogInformation("EventListener Received HeartbeatMessage: {@message}", m);
            return Task.CompletedTask;
        });

        eventHub.SubscribeToAll((eventName, ct) =>
        {
            logger.LogInformation("EventListener Received Event: {eventName}", eventName);
            return Task.CompletedTask;
        });
    }



}