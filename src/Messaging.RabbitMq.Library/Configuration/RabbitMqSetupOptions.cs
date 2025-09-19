namespace Messaging.RabbitMq.Library.Configuration;

public class RabbitMqSetupOptions
{
    public const string SectionName = "RabbitMqSetupOptions";

    public bool UseLegacyMapping { get; set; } = true;
    public bool DeclareExchanges { get; set; } = true;
    public bool UseDebugLogging { get; set; } = true;
    public bool AutoPurge { get; set; } = true;

}