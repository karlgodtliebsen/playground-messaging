using Messaging.Library;

namespace Messaging.Domain.Library.DemoMessages;

public class HeartbeatMessage : MessageBase
{
    public override string ExchangeName { get; set; } = "diagnostics";
    public override string RoutingKey { get; set; } = "diagnostics.heartbeat";// $"{ExchangeName}.heartbeat.{ApplicationName}.{MachineName}";
    public override string BindingPattern { get; set; } = "diagnostics.heartbeat.#";
    public override string? QueueName { get; set; } = null;

    public HeartbeatMessage(string machineName, string appName, DateTimeOffset dateTime)
    {
        MachineName = machineName;
        ApplicationName = appName;
        TimeStamp = dateTime;
    }
    public HeartbeatMessage()
    {
    }
}