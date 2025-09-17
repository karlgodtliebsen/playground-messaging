namespace Messaging.RabbitMq.Library;

public class PingMessage
{
    //protected override string GetExchangeName() => "diagnostics";
    //protected override string GetRoutingKey() => CreateName();

    //private string CreateName()
    //{
    //    var result = ProgramName is null || HostName is null ? BindingPattern : $"{ExchangeName}.ping.{ProgramName}.{HostName}";
    //    return result;
    //}

    public PingMessage(string hostName, string appName, DateTimeOffset dateTime)
    {
        HostName = hostName;
        ProgramName = appName;
        InternalTimeStamp = dateTime;
        PointId = hostName;
        Ident = appName;
    }

    public PingMessage()
    {
    }

    public string PointId { get; set; } = "";
    public string Ident { get; set; } = "";

    public DateTimeOffset InternalTimeStamp { get; set; }

    public string? ProgramName { get; set; } = null;

    public string? HostName { get; set; } = null;
}