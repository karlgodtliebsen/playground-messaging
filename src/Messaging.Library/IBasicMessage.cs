namespace Messaging.Library;

public interface IBasicMessage
{
    DateTimeOffset TimeStamp { get; }
    Guid CorrelationId { get; }
    string ExchangeName { get; }
    string RoutingKey { get; }
    string BindingPattern { get; }
    string? QueueName { get; }
    int Version { get; }
    string? ApplicationName { get; }
    string? MachineName { get; }
}