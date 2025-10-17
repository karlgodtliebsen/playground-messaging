using Messaging.Library.Configuration;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Messaging.Kafka.Library.Configuration;

public static class KafkaConfigurator
{

    public static IServiceCollection AddKafkaServices(this IServiceCollection services, IConfiguration configuration)
    {
        var options = configuration.GetSection(KafkaOptions.SectionName).Get<KafkaOptions>();
        if (options is null)
        {
            options = new KafkaOptions();
        }
        services.TryAddSingleton(Options.Create(options));

        var appOptions = configuration.GetSection(ApplicationInformationOptions.SectionName).Get<ApplicationInformationOptions>();
        if (appOptions is null)
        {
            appOptions = new ApplicationInformationOptions();
        }
        services.TryAddSingleton(Options.Create(appOptions));

        services.AddObservability(configuration, appOptions, useOtelLoggingProvider: false);
        services.AddActivitySourceLogging(configuration);

        return services;
    }

}