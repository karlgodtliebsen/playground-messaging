using Messaging.Library;

namespace Messaging.Domain.Library.DemoMessages;

public class PingMessage : MessageBase
{
    public override string ExchangeName { get; set; } = "diagnostics";
    public override string RoutingKey { get; set; } = "diagnostics.ping";// $"{ExchangeName}.ping.{ApplicationName}.{MachineName}";
    public override string BindingPattern { get; set; } = "diagnostics.ping.#";
    public override string? QueueName { get; set; } = null;

    public PingMessage(string machineName, string appName, DateTimeOffset dateTime)
    {
        MachineName = machineName;
        ApplicationName = appName;
        TimeStamp = dateTime;
    }

    public PingMessage()
    {
    }
}