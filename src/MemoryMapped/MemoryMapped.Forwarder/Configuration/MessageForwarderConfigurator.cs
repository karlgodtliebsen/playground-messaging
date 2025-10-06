using MemoryMapped.Forwarder.WorkerServices;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace MemoryMapped.Forwarder.Configuration;

public static class MessageForwarderConfigurator
{

    public static IServiceCollection AddMessageForwarderServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.TryAddTransient<IMessageMemoryMappedShippingWorker, MessageMemoryMappedShippingWorker>();
        services.TryAddTransient<IMessageForwarder, MessageForwarder>();
        return services;
    }
    public static IServiceCollection AddMessageForwarderHostServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.TryAddSingleton<MessageShippingServiceHost>();
        services.AddHostedService<MessageShippingServiceHost>();
        return services;
    }
}