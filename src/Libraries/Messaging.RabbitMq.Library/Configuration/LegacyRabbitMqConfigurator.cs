using Messaging.EventHub.Library.Configuration;
using Messaging.Library.Configuration;
using Messaging.Observability.Library.Configuration;
using Messaging.RabbitMq.Library.LegacySupport;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

using Wolverine;
using Wolverine.RabbitMQ.Internal;

namespace Messaging.RabbitMq.Library.Configuration;

public static class LegacyRabbitMqConfigurator
{
    private const string Consumer = "consumer";
    private const string Producer = "producer";
    private const string Monitor = "monitor";

    public static IServiceCollection AddLegacyRabbitMqServices(this IServiceCollection services, IConfiguration configuration)
    {
        var options = configuration.GetSection(RabbitMqOptions.SectionName).Get<RabbitMqOptions>();
        var setupOptions = configuration.GetSection(RabbitMqSetupOptions.SectionName).Get<RabbitMqSetupOptions>();
        if (options is null)
        {
            options = new RabbitMqOptions();
        }
        services.TryAddSingleton(Options.Create(options));
        if (setupOptions is null)
        {
            setupOptions = new RabbitMqSetupOptions();
        }
        setupOptions.UseLegacyMapping = true;
        services.TryAddSingleton(Options.Create(setupOptions));
        var appOptions = configuration.GetSection(ApplicationInformationOptions.SectionName).Get<ApplicationInformationOptions>();
        if (appOptions is null)
        {
            appOptions = new ApplicationInformationOptions();
        }
        services.TryAddSingleton(Options.Create(appOptions));

        services.TryAddKeyedSingleton(Monitor, Options.Create(Array.Empty<string>()));


        services.TryAddSingleton<IRabbitMqEnvelopeMapper, RabbitMqHeaderEnrich>();
        //service.TryAddSingleton<TypeToQueueRegistry>();
        services.AddEventHubServices(configuration);
        services.AddObservability(configuration, appOptions, useOtelLoggingProvider: false);
        services.AddActivitySourceLogging(configuration);

        return services;
    }

}