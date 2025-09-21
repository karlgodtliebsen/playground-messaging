using Messaging.Library;

using Wolverine.RabbitMQ;
using Wolverine.RabbitMQ.Internal;

namespace Messaging.RabbitMq.Library.MessageSupport;

public class MessageMap : IMessageMap
{
    public MessageMap()
    {
    }
    public MessageMap(Type messageType, string exchangeName, string routingKey, string bindingPattern, string? queueName = null)
    {
        MessageType = messageType;
        ExchangeName = exchangeName;
        RoutingKey = routingKey;
        BindingPattern = bindingPattern;
        QueueName = queueName;
    }

    public string ExchangeName { get; set; } = null!;
    public string RoutingKey { get; set; } = null!;
    public string BindingPattern { get; set; } = null!;
    public string? QueueName { get; set; }
    public QueueType QueueType { get; set; } = QueueType.classic;

    public ExchangeType ExchangeType { get; set; } = ExchangeType.Topic;
    public Type MessageType { get; init; } = null!;
    public bool UseExchange { get; set; } = true;
    public bool DurableExchange { get; set; } = true;
    public bool DurableQueue { get; set; } = true;
    public bool UseQueue { get; set; } = true;
    public bool UseHeaderMapping { get; set; } = true;

    public bool Exclusive { get; set; } = false;
    public bool NoWait { get; set; } = false;
    public bool AutoDeleteQueue { get; set; } = false;
    public bool AutoDeleteExchange { get; set; } = false;

    public bool Mandatory { get; set; } = true;
    public bool SupportLegacy { get; set; } = true;

    public bool AutoAcknowledge { get; set; } = false;

    public int QueueFullWaitTime { get; set; } = 2000;

    public int TimeToLive { get; set; } = 60 * 60 * 24 * 7;

    public bool PurgeOnStartup { get; set; } = false;

}

public class MessageMap<T> : MessageMap where T : new()
{
    public MessageMap()
    {
        MessageType = typeof(T);
        var msg = new T();
        if (msg is IMessageBase m)
        {
            ExchangeName = m.ExchangeName;
            RoutingKey = m.RoutingKey;
            BindingPattern = m.BindingPattern;
            QueueName = m.QueueName;
        }
    }
    public MessageMap(string queueName)
    {
        MessageType = typeof(T);
        var msg = new T();
        if (msg is IMessageBase m)
        {
            ExchangeName = m.ExchangeName;
            RoutingKey = m.RoutingKey;
            BindingPattern = m.BindingPattern;
        }
        QueueName = queueName;
    }

    public MessageMap(string exchangeName, string routingKey, string bindingPattern, string? queueName = null)
    {
        MessageType = typeof(T);
        ExchangeName = exchangeName;
        RoutingKey = routingKey;
        BindingPattern = bindingPattern;
        QueueName = queueName;
    }
}