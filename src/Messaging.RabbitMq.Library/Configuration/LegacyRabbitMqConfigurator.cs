using Messaging.Library.Configuration;
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

    public static IServiceCollection AddLegacyRabbitMqServices(this IServiceCollection service, IConfiguration configuration)
    {
        var options = configuration.GetSection(RabbitMqOptions.SectionName).Get<RabbitMqOptions>();
        var setupOptions = configuration.GetSection(RabbitMqSetupOptions.SectionName).Get<RabbitMqSetupOptions>();
        if (options is null)
        {
            options = new RabbitMqOptions();
        }
        service.TryAddSingleton(Options.Create(options));
        if (setupOptions is null)
        {
            setupOptions = new RabbitMqSetupOptions();
        }
        setupOptions.UseLegacyMapping = true;
        service.TryAddSingleton(Options.Create(setupOptions));
        service.TryAddKeyedSingleton(Monitor, Options.Create(Array.Empty<string>()));
        service.TryAddSingleton<IRabbitMqEnvelopeMapper, RabbitMqHeaderEnrich>();
        service.TryAddSingleton<TypeToQueueMapper>();
        service.AddEventHubServices(configuration);
        return service;
    }

}