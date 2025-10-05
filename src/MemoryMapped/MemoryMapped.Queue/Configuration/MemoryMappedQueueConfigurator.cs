using MemoryMapped.Queue.Monitor;
using MemoryMapped.Queue.Serializers;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;


namespace MemoryMapped.Queue.Configuration;

public static class MemoryMappedQueueConfigurator
{

    public static IServiceCollection AddMemoryMappedQueueServices(this IServiceCollection services, IConfiguration configuration)
    {
        var cfg = configuration.GetSection(MemoryMappedOptions.SectionName).Get<MemoryMappedOptions>();
        if (cfg is null)
        {
            cfg = new MemoryMappedOptions() { Name = Ulid.NewUlid(DateTimeOffset.UtcNow).ToString() };
        }
        var options = Options.Create(cfg);
        return services.AddMemoryMappedQueueServices(options);
    }

    public static IServiceCollection AddMemoryMappedQueueServices(this IServiceCollection services, IOptions<MemoryMappedOptions> mmOptions)
    {
        if (mmOptions is null) throw new ArgumentNullException(nameof(mmOptions), MemoryMappedOptions.SectionName);
        services.TryAddSingleton(mmOptions);

        var monOptions = Options.Create(new MonitoringOptions());
        services.TryAddSingleton(monOptions);

        services.TryAddTransient<IMemoryMappedQueue, MemoryMappedQueue>();
        //services.TryAddTransient<IFastSerializer, FastMemoryPackSerializer>();
        services.TryAddTransient<IFastSerializer, FastJsonSerializer>();

        services.TryAddTransient<IMemoryMappedQueueMonitor, MemoryMappedQueueMonitor>();
        return services;
    }
}