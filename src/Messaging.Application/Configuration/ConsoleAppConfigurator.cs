using MemoryMapped.Queue.Configuration;

using Messaging.Application.Services.Workers;
using Messaging.Domain.Library.Services;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Messaging.Application.Configuration;

public static class ConsoleAppConfigurator
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection service, IConfiguration configuration)
    {
        service.TryAddSingleton<EventHubListener>();
        service.AddMemoryMappedQueueServices(configuration);
        service.TryAddScoped<MessagingProducerWorkerService>();
        service.TryAddScoped<DiagnosticsMessagingProducerWorkerService>();
        service.TryAddScoped<SimpleMessagingProducerWorkerService>();
        service.TryAddScoped<MessagingConsumerWorkerService>();        //IMemoryMappedQueue
        return service;
    }

    public static IServiceProvider SetupEventListener(this IServiceProvider serviceProvider)
    {
        var listener = serviceProvider.GetRequiredService<EventHubListener>();
        listener.SetupSubscriptions();
        return serviceProvider;
    }
}