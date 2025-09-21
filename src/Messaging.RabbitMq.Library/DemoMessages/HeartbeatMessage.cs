using Messaging.Library;

namespace Messaging.RabbitMq.Library.DemoMessages;

public class HeartbeatMessage : IBasicMessage
{
    public string ExchangeName { get; } = "diagnostics";
    public string RoutingKey { get; } = "diagnostics.heartbeat";// $"{ExchangeName}.heartbeat.{ApplicationName}.{MachineName}";
    public string BindingPattern { get; } = "diagnostics.heartbeat.#";

    public HeartbeatMessage(string machineName, string appName, DateTimeOffset dateTime)
    {
        MachineName = machineName;
        ApplicationName = appName;
        TimeStamp = dateTime;
    }

    public HeartbeatMessage()
    {
    }

    public DateTimeOffset TimeStamp { get; set; }
    public Guid CorrelationId { get; set; } = Guid.CreateVersion7(DateTimeOffset.UtcNow);
    public string? QueueName { get; set; } = null;
    public string? ApplicationName { get; set; } = null;
    public string? MachineName { get; set; } = null;
    public int Version { get; set; } = 1;
}