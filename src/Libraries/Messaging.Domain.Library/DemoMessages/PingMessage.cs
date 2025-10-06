using Messaging.Library;

namespace Messaging.Domain.Library.DemoMessages;

public class PingMessage : MessageBase
{

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