using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Messaging.Kafka.Library.Configuration;

public static class KafkaConfigurator
{

    public static IServiceCollection AddKafkaServices(this IServiceCollection service, IConfiguration configuration)
    {
        var options = configuration.GetSection(KafkaOptions.SectionName).Get<KafkaOptions>();
        if (options is null)
        {
            options = new KafkaOptions();
        }
        service.TryAddSingleton(Options.Create(options));
        return service;
    }

}