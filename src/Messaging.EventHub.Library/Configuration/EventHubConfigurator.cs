using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using System.Diagnostics.Metrics;

namespace Messaging.EventHub.Library.Configuration;

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
        // service.TryAddSingleton<IEventHub, EventHub>();


        if (options.EnableMetrics)
        {
            service.AddSingleton<EventHubMetrics>((sp) =>
                new EventHubMetrics(sp.GetRequiredService<IMeterFactory>(), options.MetricsName));

            service.AddOpenTelemetry()
                .WithMetrics(mb =>
                {
                    mb.AddMeter(options.MetricsName); // <-- IMPORTANT: listen to your meter name
                    //mb.AddRuntimeInstrumentation(); // optional

                    //mb.AddAspNetCoreInstrumentation(); // optional
                    //mb.AddHttpClientInstrumentation(); // optional

                    //mb.AddPrometheusExporter(); // exporter

                    //mb.AddOtlpExporter(otlp =>
                    //{
                    //    otlp.Endpoint = new Uri("http://otel-collector:4317"); // gRPC default
                    //    // otlp.Protocol = OtlpExportProtocol.Grpc; // optional (default)
                    //});

                });

            service.TryAddSingleton<IEventHub>(sp => new EventHub(sp.GetRequiredService<EventHubMetrics>(), sp.GetRequiredService<IOptions<EventHubOptions>>(), sp.GetRequiredService<ILogger<EventHub>>()));
        }
        else
        {
            service.TryAddSingleton<IEventHub>(sp => new EventHub(sp.GetRequiredService<IOptions<EventHubOptions>>(), sp.GetRequiredService<ILogger<EventHub>>()));
        }
        return service;
    }

}

