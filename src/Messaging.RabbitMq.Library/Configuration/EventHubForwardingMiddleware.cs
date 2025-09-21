using Messaging.Library.EventHubChannel;

using Wolverine;

using IMessage = Messaging.Library.IMessage;

namespace Messaging.RabbitMq.Library.Configuration;

// ReSharper disable once ClassNeverInstantiated.Global
public class EventHubForwardingMiddleware(IEventHub eventHub)
{
    public void Before(Envelope envelope)
    {
        if (envelope.Message is IMessage message)//can be removed to allow for all types
        {
            eventHub.Publish(message);
        }
    }

}
