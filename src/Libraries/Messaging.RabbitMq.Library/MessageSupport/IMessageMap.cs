using Wolverine.RabbitMQ;
using Wolverine.RabbitMQ.Internal;

namespace Messaging.RabbitMq.Library.MessageSupport;

public interface IMessageMap
{
    ExchangeType ExchangeType { get; set; }
    QueueType QueueType { get; set; }
    Type MessageType { get; }

    bool UseExchange { get; set; }
    bool DurableExchange { get; set; }
    bool DurableQueue { get; set; }
    bool UseQueue { get; set; }
    bool UseHeaderMapping { get; set; }
    string ExchangeName { get; set; }
    string BindingPattern { get; set; }
    string RoutingKey { get; set; }
    string? QueueName { get; set; }
    bool Exclusive { get; set; }
    bool NoWait { get; set; }
    bool AutoDeleteQueue { get; set; }
    bool AutoDeleteExchange { get; set; }
    bool Mandatory { get; set; }
    bool SupportLegacy { get; set; }
    bool AutoAcknowledge { get; set; }
    int QueueFullWaitTime { get; set; }
    int TimeToLive { get; set; }
    bool PurgeOnStartup { get; set; }
}