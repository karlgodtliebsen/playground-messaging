using Messaging.Library.EventHubChannel;

namespace Messaging.RabbitMq.Library.Configuration;


public class GenericMessageForwarder<T> where T : class
{
    public static void Handle(T message, IEventHub eventHub)
    {
        eventHub.Publish(message);
    }
}