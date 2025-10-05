namespace Messaging.RabbitMq.Library.Configuration;

public class RabbitMqSetupOptions
{
    public const string SectionName = "RabbitMqSetupOptions";

    public bool UseEventPublishing { get; set; } = true;
    public bool UseLegacyMapping { get; set; } = false;
    public bool DeclareExchanges { get; set; } = true;
    public bool UseDebugLogging { get; set; } = true;
    public bool AutoPurge { get; set; } = true;

}