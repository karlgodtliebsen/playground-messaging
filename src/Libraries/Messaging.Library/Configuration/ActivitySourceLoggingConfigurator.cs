using Messaging.Library.ActivitySourceLogging;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using System.Diagnostics;

namespace Messaging.Library.Configuration;

public static class ActivitySourceLoggingConfigurator
{
    private static readonly ActivityListener Listener = new() { ShouldListenTo = _ => true, Sample = (ref ActivityCreationOptions<ActivityContext> options) => ActivitySamplingResult.AllData };

    public static IServiceCollection AddActivitySourceLogging(this IServiceCollection services, IConfiguration configuration, string? key = null, Func<ActivityListener>? addListener = null)
    {
        services.TryAddSingleton<IActivitySourceFactory, ActivitySourceFactory>();
        if (key is not null)
        {
            services.TryAddKeyedSingleton<IActivitySourceFactory, ActivitySourceFactory>(key);
        }

        var activityListener = addListener is null
            ? Listener
            : addListener();
        ActivitySource.AddActivityListener(activityListener); //only adds non-existing listeners, so idempotent, safe for multiple calls
        return services;
    }
}