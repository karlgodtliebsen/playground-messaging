using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Maui.Prometheus.Viewer.Configuration;

public static class PrometheusConfigurator
{
    public static IServiceCollection AddPrometheusServices(this IServiceCollection services, IConfiguration configuration)
    {
        var options = configuration.GetSection(PrometheusOptions.SectionName).Get<PrometheusOptions>();
        if (options is null)
        {
            options = new PrometheusOptions();
        }
        services.TryAddSingleton(Options.Create(options));


        // Register Named Http Client For Prometheus Service
        services.AddHttpClient("PrometheusClient", client =>
        {
            client.BaseAddress = new Uri(options.Endpoint);
            client.DefaultRequestHeaders.Add("Accept", "application/json");
        });

        services.AddSingleton<IPrometheusService, PrometheusService>();
        services.AddTransient<DashboardViewModel>();

        // Register Pages
        services.AddTransient<DashboardPage>();
        services.AddTransient<SettingsPage>();

        return services;
    }
}