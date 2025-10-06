namespace Messaging.RabbitMq.Library.MessageSupport;

public interface IMessageMetaData
{
    Type MessageType { get; }
    string ExchangeName { get; }
    string RoutingKey { get; }
    string BindingPattern { get; }
    string? QueueName { get; }
}