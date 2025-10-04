using Messaging.Console.App.Services.Workers;
using Messaging.Domain.Library.Services;
using Messaging.EventHub.Library.Configuration;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Messaging.Console.App.Configuration.KafkaSupport;

public static class KafkaServicesConfigurator
{
    public static IServiceCollection AddKafkaApplicationServices(this IServiceCollection service, IConfiguration configuration)
    {
        service.TryAddSingleton<MessagingProducerWorkerService>();
        service.TryAddSingleton<DiagnosticsMessagingProducerWorkerService>();
        service.TryAddSingleton<SimpleMessagingProducerWorkerService>();
        service.TryAddSingleton<MessagingConsumerWorkerService>();
        service
            .AddEventHubServices(configuration)
            .TryAddSingleton<EventHubListener>();
        return service;
    }

    public static IServiceProvider UseKafkaEventListener(this IServiceProvider serviceProvider)
    {
        var listener = serviceProvider.GetRequiredService<EventHubListener>();
        listener.SetupSubscriptions();
        return serviceProvider;
    }
}