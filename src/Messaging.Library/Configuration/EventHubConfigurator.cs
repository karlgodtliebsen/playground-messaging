using Messaging.Library.EventHubChannel;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Messaging.Library.Configuration;

public static class EventHubConfigurator
{
    public static IServiceCollection AddEventHubServices(this IServiceCollection service, IConfiguration configuration)
    {
        var options = configuration.GetSection(EventHubOptions.SectionName).Get<EventHubOptions>();
        if (options is null)
        {
            options = new EventHubOptions();
        }
        service.TryAddSingleton(Options.Create(options));
        service.TryAddSingleton<IEventHub, EventHub>();
        return service;
    }
}