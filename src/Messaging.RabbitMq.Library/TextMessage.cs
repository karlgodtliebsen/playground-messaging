using Messaging.Library;

namespace Messaging.RabbitMq.Library;

public class TextMessage : IBasicMessage
{

    public string ExchangeName { get; } = "text-message";
    public string RoutingKey { get; } = "text-message.route";
    public string BindingPattern { get; } = "text-message.#";
    public string? QueueName { get; set; } = null;


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
    public string? MachineName { get; set; }
    public string? ApplicationName { get; set; }
    public DateTimeOffset TimeStamp { get; set; }
    public Guid CorrelationId { get; set; } = Guid.CreateVersion7(DateTimeOffset.UtcNow);

    public string MessageData { get; set; } = "";
    public int Version { get; set; } = 1;
}