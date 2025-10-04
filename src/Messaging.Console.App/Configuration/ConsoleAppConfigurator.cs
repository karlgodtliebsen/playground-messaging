using Messaging.Domain.Library.Services;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Messaging.Console.App.Configuration;

public static class ConsoleAppConfigurator
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection service, IConfiguration configuration)
    {
        service.TryAddSingleton<EventHubListener>();
        return service;
    }

    public static IServiceProvider SetupEventListener(this IServiceProvider serviceProvider)
    {
        var listener = serviceProvider.GetRequiredService<EventHubListener>();
        listener.SetupSubscriptions();
        return serviceProvider;
    }
}