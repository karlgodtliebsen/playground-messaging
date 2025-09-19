namespace Messaging.RabbitMq.Library.Configuration;

public class RabbitMqSetupOptions
{
    public bool UseLegacyMapping { get; set; } = true;
    public bool DeclareExchanges { get; set; } = true;
    public bool UseDebugLogging { get; set; } = true;

}