
using Messaging.EventHub.Library;

using Microsoft.Extensions.Logging;

using Wolverine;


namespace Messaging.RabbitMq.Library.Configuration;

// ReSharper disable once ClassNeverInstantiated.Global
public class EventHubForwardingMiddleware(IEventHub eventHub, ILogger<EventHubForwardingMiddleware> logger)
{
    public void Before(Envelope envelope)
    {
        if (envelope.Message is not null)//IMessage can be removed to allow for all types or a filter can be defined and handled using configuration
        {
            var message = envelope.Message;
            if (logger.IsEnabled(LogLevel.Trace)) logger.LogTrace("Forwarding MessageBus Message to EventHub: {@message}", message);
            eventHub.Publish(message);
        }
    }
}
