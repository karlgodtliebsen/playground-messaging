using Messaging.Library;

namespace Messaging.RabbitMq.Library.DemoMessages;

public class PingMessage : IBasicMessage
{
    public string ExchangeName { get; } = "diagnostics";
    public string RoutingKey { get; } = "diagnostics.ping";// $"{ExchangeName}.ping.{ApplicationName}.{MachineName}";
    public string BindingPattern { get; } = "diagnostics.ping.#";

    public PingMessage(string machineName, string appName, DateTimeOffset dateTime)
    {
        MachineName = machineName;
        ApplicationName = appName;
        TimeStamp = dateTime;
    }

    public PingMessage()
    {
    }
    public DateTimeOffset TimeStamp { get; set; }
    public Guid CorrelationId { get; set; } = Guid.CreateVersion7(DateTimeOffset.UtcNow);
    public string? QueueName { get; set; } = null;
    public string? ApplicationName { get; set; } = null;

    public string? MachineName { get; set; } = null;
    public int Version { get; set; } = 1;
}