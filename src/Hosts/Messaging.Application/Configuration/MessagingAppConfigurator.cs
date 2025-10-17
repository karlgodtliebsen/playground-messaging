using Messaging.Application.Services.Workers;
using Messaging.Domain.Library.Services;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Messaging.Application.Configuration;

public static class MessagingAppConfigurator
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection service, IConfiguration configuration)
    {
        service.TryAddSingleton<EventHubListener>();
        //service.AddMemoryMappedQueueServices(configuration);
        service.TryAddScoped<OrderDomainProducerWorkerService>();
        service.TryAddScoped<DiagnosticsMessagingProducerWorkerService>();
        service.TryAddScoped<SimpleMessagingProducerWorkerService>();
        service.TryAddScoped<MessagingConsumerWorkerService>();
        return service;
    }

    public static IServiceProvider SetupEventListener(this IServiceProvider serviceProvider)
    {
        var listener = serviceProvider.GetRequiredService<EventHubListener>();
        listener.SetupSubscriptions();
        return serviceProvider;
    }
}