using Messaging.EventHub.Library.Configuration;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Maui.Prometheus.Viewer.Configuration;

public static class PrometheusViewerConfigurator
{
    public static IServiceCollection AddPrometheusServices(this IServiceCollection services, IConfiguration configuration)
    {
        var options = configuration.GetSection(PrometheusOptions.SectionName).Get<PrometheusOptions>();
        if (options is null)
        {
            options = new PrometheusOptions();
        }
        services.TryAddSingleton(Options.Create(options));
        services.TryAddSingleton(new CancellationTokenSource());//Must be used when exiting app

        services.AddEventHubServices(configuration);
        // Register Named Http Client For Prometheus Service
        services.AddHttpClient("PrometheusClient", client =>
        {
            client.BaseAddress = new Uri(options.Endpoint);
            client.DefaultRequestHeaders.Add("Accept", "application/json");
        });

        services.AddSingleton<IPrometheusService, PrometheusService>();
        // Register Pages with models
        services.AddTransient<DashboardPage>();
        services.AddTransient<DashboardViewModel>();

        services.AddTransient<SystemMetricsPage>();
        services.AddTransient<SystemMetricsViewModel>();

        services.AddTransient<SettingsViewModel>();
        services.AddTransient<SettingsPage>();

        services.AddTransient<EventHubDetailViewModel>();
        services.AddTransient<EventHubDetailPage>();
        return services;
    }
}