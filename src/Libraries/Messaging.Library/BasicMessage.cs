namespace Messaging.Library;

public interface IEventMessage
{

}
public interface IMessage
{
}

public interface IMessageBase : IMessage
{
    string ExchangeName { get; }
    string RoutingKey { get; }
    string BindingPattern { get; }
    string? QueueName { get; }
    DateTimeOffset TimeStamp { get; }
    Guid CorrelationId { get; }
    int Version { get; }
    string? ApplicationName { get; }
    string? MachineName { get; }

}


public abstract class MessageBase : IMessageBase
{
    public abstract string ExchangeName { get; set; }
    public abstract string RoutingKey { get; set; }
    public abstract string BindingPattern { get; set; }
    public abstract string? QueueName { get; set; }
    public string? MachineName { get; set; }
    public string? ApplicationName { get; set; }
    public DateTimeOffset TimeStamp { get; set; } = DateTimeOffset.UtcNow;
    public Guid CorrelationId { get; set; } = Guid.CreateVersion7(DateTimeOffset.UtcNow);
    public int Version { get; set; } = 1;
}
