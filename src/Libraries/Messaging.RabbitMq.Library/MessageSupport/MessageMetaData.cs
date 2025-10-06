namespace Messaging.RabbitMq.Library.MessageSupport;

public class MessageMetaData : IMessageMetaData
{
    public Type MessageType { get; init; } = null!;
    public string ExchangeName { get; init; } = null!;
    public string RoutingKey { get; init; } = null!;
    public string BindingPattern { get; init; } = null!;
    public string? QueueName { get; init; } = null!;
}

public class MessageMetaData<T> : MessageMetaData
{
    public MessageMetaData()
    {
        base.MessageType = typeof(T);
    }
}