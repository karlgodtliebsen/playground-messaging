using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Messaging.Library.Configuration;

public static class MessagingLibraryConfigurator
{
    public static IServiceCollection AddLibraryServices(this IServiceCollection service, IConfiguration configuration)
    {
        service.TryAddSingleton<ISignalChannel, SignalChannel>();
        return service;
    }
}
