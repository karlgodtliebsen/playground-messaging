namespace Messaging.RabbitMq.Library;

public interface IMessage
{
    DateTimeOffset InternalTimeStamp { get; }
    string ExchangeName { get; }
    string RoutingKey { get; }
    string BindingPattern { get; }
    string? QueueName { get; }

}