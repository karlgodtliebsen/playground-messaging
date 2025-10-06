using Messaging.Library;

namespace Messaging.Domain.Library.DemoMessages;

public class HeartbeatMessage : MessageBase
{
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