namespace Messaging.Library;

public interface IMessageBase : IMessage
{
    Guid Id { get; }
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