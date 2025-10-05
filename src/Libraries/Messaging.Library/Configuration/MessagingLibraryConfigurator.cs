using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Messaging.Library.Configuration;

public static class MessagingLibraryConfigurator
{
    public static IServiceCollection AddMessagingLibraryServices(this IServiceCollection service, IConfiguration configuration)
    {
        return service;
    }
}