namespace Messaging.RabbitMq.Library;

public class TextMessage : IMessage
{

    public string ExchangeName { get; } = "text-message";
    public string RoutingKey { get; } = "text-message.route";
    public string BindingPattern { get; } = "text-message.#";


    public TextMessage(string hostName, string appName, string messageData, DateTimeOffset dateTime)
    {
        HostName = hostName;
        AppName = appName;
        InternalTimeStamp = dateTime;
        MessageData = messageData;
    }

    public TextMessage()
    {
    }
    public string? QueueName { get; set; } = null;
    public string HostName { get; set; }
    public string AppName { get; set; }
    public DateTimeOffset InternalTimeStamp { get; set; }

    public string MessageData { get; set; } = "";
    public int Version { get; set; } = 1;

    /*

     {
    "ProgramName":"Next Generation Messages",
    "HostName":"KGO-P16G2-2401",
    "TrackingId":"019959b8-21eb-75fb-b514-066f9b94f2d5",
    "CorrelationId":"019959b8-21eb-7d3a-98c9-ed18391e1d9f",
    "OriginEndpoint":"localhost",
    "OriginPrefix":"go.main",
    "OriginQueueName":"","OriginPort":5672,
    "ExchangeName":"diagnostics",
    "SubscriptionTopic":"diagnostics",
    "RoutingKey":"diagnostics.ping.next generation messages.kgo-p16g2-2401",
    "QueueName":"",
    "Ident":"Ping-01K5CVG8FC6TST47H7A7G35SAQ",
    "PointId":"KGO-P16G2-2401",
    "InternalTimeStamp":"2025-09-17T22:07:43.5957631+00:00"}
     */

}