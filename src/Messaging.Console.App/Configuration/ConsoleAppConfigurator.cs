using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Messaging.Console.App.Configuration;

public static class ConsoleAppConfigurator
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection service, IConfiguration configuration)
    {
        service.TryAddSingleton<SignalListener>();
        return service;
    }

    public static IServiceProvider SetupChannelListener(this IServiceProvider serviceProvider)
    {
        var listener = serviceProvider.GetRequiredService<SignalListener>();
        listener.SetupSubscriptions();
        return serviceProvider;
    }
}