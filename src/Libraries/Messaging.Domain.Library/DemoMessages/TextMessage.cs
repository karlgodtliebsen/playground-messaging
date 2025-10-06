using Messaging.Library;

namespace Messaging.Domain.Library.DemoMessages;

public class TextMessage : MessageBase
{

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

    public string Identity { get; set; } = Ulid.NewUlid(DateTimeOffset.UtcNow).ToString();

    public string MessageData { get; set; } = "";
}