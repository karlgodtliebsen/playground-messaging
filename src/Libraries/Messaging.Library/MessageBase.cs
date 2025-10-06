namespace Messaging.Library;

public abstract class MessageBase : IMessageBase
{
    public Guid Id { get; set; } = Guid.CreateVersion7(DateTimeOffset.UtcNow);
    //public abstract string ExchangeName { get; set; }
    //public abstract string RoutingKey { get; set; }
    //public abstract string BindingPattern { get; set; }
    //public abstract string? QueueName { get; set; }
    public string? MachineName { get; set; }
    public string? ApplicationName { get; set; }
    public DateTimeOffset TimeStamp { get; set; } = DateTimeOffset.UtcNow;
    public Guid CorrelationId { get; set; } = Guid.CreateVersion7(DateTimeOffset.UtcNow);
    public int Version { get; set; } = 1;
}
