using Wolverine.RabbitMQ;
using Wolverine.RabbitMQ.Internal;

namespace Messaging.RabbitMq.Library.MessageSupport;

public class MessageTypeMap : IMessageTypeMap
{

    public MessageTypeMap(IMessageMetaData md)
    {
        MetaData = md;
    }

    public MessageTypeMap(Type messageType, string exchangeName, string routingKey, string bindingPattern, string? queueName = null)
    {
        var md = new MessageMetaData()
        {
            MessageType = messageType,
            ExchangeName = exchangeName,
            RoutingKey = routingKey,
            BindingPattern = bindingPattern,
            QueueName = queueName
        };
        MetaData = md;
    }

    public IMessageMetaData MetaData { get; set; }

    public Type MessageType => MetaData.MessageType;
    public string ExchangeName => MetaData.ExchangeName;
    public string RoutingKey => MetaData.RoutingKey;
    public string BindingPattern => MetaData.BindingPattern;
    public string? QueueName => MetaData.QueueName;

    public QueueType QueueType { get; set; } = QueueType.classic;

    public ExchangeType ExchangeType { get; set; } = ExchangeType.Topic;
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

