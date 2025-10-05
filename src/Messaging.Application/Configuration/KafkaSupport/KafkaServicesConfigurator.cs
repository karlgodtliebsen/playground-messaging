using Messaging.Application.Services.Workers;
using Messaging.Domain.Library.Services;
using Messaging.EventHub.Library.Configuration;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Messaging.Application.Configuration.KafkaSupport;

public static class KafkaServicesConfigurator
{
    public static IServiceCollection AddKafkaApplicationServices(this IServiceCollection service, IConfiguration configuration)
    {
        service.TryAddScoped<MessagingProducerWorkerService>();
        service.TryAddScoped<DiagnosticsMessagingProducerWorkerService>();
        service.TryAddScoped<SimpleMessagingProducerWorkerService>();
        service.TryAddScoped<MessagingConsumerWorkerService>();
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