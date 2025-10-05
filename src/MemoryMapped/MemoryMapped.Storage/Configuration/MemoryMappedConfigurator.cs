using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MemoryMapped.Storage.Configuration;

public static class MemoryMappedConfigurator
{

    public static IServiceCollection AddMemoryMappedServices(this IServiceCollection services, IConfiguration configuration)
    {
        //services.TryAddSingleton<IMemoryMappedQueue, MemoryMappedQueue>();
        //services.AddMemoryMappedQueueServices(configuration);
        return services;
    }

    public static IServiceCollection AddMemoryMappedServices(this IServiceCollection services/*, IOptions<MemoryMappedOptions> options*/)
    {
        //    services.AddMemoryMappedQueueServices(options);
        //    IFastSerializer serializer = new FastMemoryPackSerializer();
        //    services.TryAddSingleton<IMemoryMappedQueue>((sp) => new MemoryMappedQueue(options, serializer));
        return services;
    }
}