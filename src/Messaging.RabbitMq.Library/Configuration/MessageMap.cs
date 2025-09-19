using Wolverine.RabbitMQ;
using Wolverine.RabbitMQ.Internal;

namespace Messaging.RabbitMq.Library.Configuration;

public class MessageMap<T> : IMessageMap where T : IMessage, new()
{
    public MessageMap()
    {
        var t = new T();
        Exchange = t.ExchangeName;
        BindingPattern = t.BindingPattern;
        RoutingKey = t.RoutingKey;
        QueueName = t.QueueName;
    }

    public QueueType QueueType { get; set; } = QueueType.classic;

    public ExchangeType ExchangeType { get; set; } = ExchangeType.Topic;
    public Type MessageType => typeof(T);
    public bool UseExchange { get; set; } = true;
    public bool DurableExchange { get; set; } = true;
    public bool DurableQueue { get; set; } = true;
    public bool UseQueue { get; set; }
    public bool UseHeaderMapping { get; set; } = true;
    public string Exchange { get; set; }
    public string BindingPattern { get; set; }
    public string RoutingKey { get; set; }
    public string? QueueName { get; set; }
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