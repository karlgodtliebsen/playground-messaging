using Messaging.Library;

namespace Messaging.Domain.Library.DemoMessages;

public class TextMessage : MessageBase
{
    public override string ExchangeName { get; set; } = "text-message";
    public override string RoutingKey { get; set; } = "text-message.route";
    public override string BindingPattern { get; set; } = "text-message.#";
    public override string? QueueName { get; set; } = null;

    public TextMessage(string machineName, string applicationName, string messageData, DateTimeOffset dateTime)
    {
        MachineName = machineName;
        ApplicationName = applicationName;
        TimeStamp = dateTime;
        MessageData = messageData;
    }
    public TextMessage()
    {
    }

    public string MessageData { get; set; } = "";
}