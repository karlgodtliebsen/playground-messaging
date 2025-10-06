using MemoryMapped.Queue;

using Messaging.Domain.Library.DemoMessages;
using Messaging.Domain.Library.Orders;
using Messaging.Domain.Library.Payments;
using Messaging.Domain.Library.SimpleMessages;
using Messaging.EventHub.Library;

using Microsoft.Extensions.Logging;

namespace Messaging.Domain.Library.Services;

public class EventHubListener(IEventHub eventHub, IMemoryMappedQueue queue, ILogger<EventHubListener> logger)
{
    public void SetupSubscriptions()
    {
        eventHub.Subscribe("Alive", (ct) =>
        {
            logger.LogInformation("EventListener Received Alive Signal");
            return Task.CompletedTask;
        });

        eventHub.Subscribe<string>("Alive", (msg, ct) =>
        {
            logger.LogInformation("EventListener Received Alive Signal from {msg}", msg);
            return Task.CompletedTask;
        });

        eventHub.Subscribe<TextMessage>((m, ct) =>
        {
            logger.LogInformation("EventListener Received TextMessage: {@message}", m);
            queue.TryEnqueue(m);
            return Task.CompletedTask;
        });

        eventHub.Subscribe<PingMessage>((m, ct) =>
        {
            logger.LogInformation("EventListener Received PingMessage: {@message}", m);
            queue.TryEnqueue(m);
            return Task.CompletedTask;
        });
        eventHub.Subscribe<HeartbeatMessage>((m, ct) =>
        {
            logger.LogInformation("EventListener Received HeartbeatMessage: {@message}", m);
            queue.TryEnqueue(m);
            return Task.CompletedTask;
        });

        eventHub.Subscribe<CreateMessage>((m, ct) =>
        {
            logger.LogInformation("EventListener Received CreateMessage: {@message}", m);
            queue.TryEnqueue(m);
            return Task.CompletedTask;
        });

        eventHub.Subscribe<InformationMessage>((m, ct) =>
        {
            logger.LogInformation("EventListener Received InformationMessage: {@message}", m);
            queue.TryEnqueue(m);
            return Task.CompletedTask;
        });
        eventHub.Subscribe<PaymentProcessed>((m, ct) =>
        {
            logger.LogInformation("EventListener Received PaymentProcessed: {@message}", m);
            queue.TryEnqueue(m);
            return Task.CompletedTask;
        });
        eventHub.Subscribe<OrderUpdated>((m, ct) =>
        {
            logger.LogInformation("EventListener Received OrderUpdated: {@message}", m);
            queue.TryEnqueue(m);
            return Task.CompletedTask;
        });
        eventHub.Subscribe<OrderCreated>((m, ct) =>
        {
            logger.LogInformation("EventListener Received OrderCreated: {@message}", m);
            queue.TryEnqueue(m);
            return Task.CompletedTask;
        });


        eventHub.SubscribeToAll((eventName, ct) =>
        {
            logger.LogInformation("EventListener Received Event: {eventName}", eventName);
            return Task.CompletedTask;
        });
    }



}