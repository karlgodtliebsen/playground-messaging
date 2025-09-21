namespace Messaging.Library;

public interface IMessage
{

}

public interface IBaseMessage : IMessage
{
    string ExchangeName { get; }
    string RoutingKey { get; }
    string BindingPattern { get; }
    string? QueueName { get; }
}

public class BaseMessage : IBaseMessage
{
    protected BaseMessage() { }
    protected BaseMessage(string exchangeName, string routingKey, string bindingPattern, string? queueName = null)
    {
        ExchangeName = exchangeName;
        RoutingKey = routingKey;
        BindingPattern = bindingPattern;
        QueueName = queueName;
    }
    public string ExchangeName { get; set; } = null!;
    public string RoutingKey { get; set; } = null!;
    public string BindingPattern { get; set; } = null!;
    public string? QueueName { get; set; }
}

public interface IBasicMessage : IBaseMessage
{
    DateTimeOffset TimeStamp { get; }
    Guid CorrelationId { get; }
    int Version { get; }
    string? ApplicationName { get; }
    string? MachineName { get; }
}

public class BasicMessage : IBasicMessage
{
    public string ExchangeName { get; set; } = null!;
    public string RoutingKey { get; set; } = null!;
    public string BindingPattern { get; set; } = null!;
    public string? QueueName { get; set; }
    public string? MachineName { get; set; }
    public string? ApplicationName { get; set; }
    public DateTimeOffset TimeStamp { get; set; }
    public Guid CorrelationId { get; set; } = Guid.CreateVersion7(DateTimeOffset.UtcNow);
    public int Version { get; set; } = 1;
}
