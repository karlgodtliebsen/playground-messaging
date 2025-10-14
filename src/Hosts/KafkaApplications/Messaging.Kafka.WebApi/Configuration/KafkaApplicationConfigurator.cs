using Messaging.Domain.Library.Services;
using Messaging.EventHub.Library.Configuration;

using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Messaging.Kafka.WebApi.Configuration;

public static class KafkaApplicationConfigurator
{

    public static IServiceCollection AddKafkaApplicationServices(this IServiceCollection service, IConfiguration configuration)
    {
        service
            .AddEventHubServices(configuration)
            .TryAddSingleton<EventHubListener>();
        return service;
    }
    public static IServiceProvider UseEventListener(this IServiceProvider serviceProvider)
    {
        var listener = serviceProvider.GetRequiredService<EventHubListener>();
        listener.SetupSubscriptions();
        return serviceProvider;
    }

}