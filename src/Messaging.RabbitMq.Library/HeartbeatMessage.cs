namespace Messaging.RabbitMq.Library;

public class HeartbeatMessage : IMessage
{
    public string ExchangeName { get; } = "diagnostics";
    public string RoutingKey { get; } = "diagnostics.heartbeat";// $"{ExchangeName}.heartbeat.{ProgramName}.{HostName}";
    public string BindingPattern { get; } = "diagnostics.heartbeat.#";

    public HeartbeatMessage(string hostName, string appName, DateTimeOffset dateTime)
    {
        HostName = hostName;
        ProgramName = appName;
        InternalTimeStamp = dateTime;
    }

    public HeartbeatMessage()
    {
    }

    public DateTimeOffset InternalTimeStamp { get; set; }
    public string? QueueName { get; set; } = null;
    public string? ProgramName { get; set; } = null;

    public string? HostName { get; set; } = null;
}