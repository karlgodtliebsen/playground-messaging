using Messaging.EventHub.Library.EventHubs;

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
        if (options.EnableMetrics)
        {
        }
        if (options.EnableUseChannel)
        {
            service.AddEventHubChannelServices(options);
        }
        else
        {
            service.AddEventHubCollectionServices(options);
        }

        return service;
    }


    private static IServiceCollection AddEventHubChannelServices(this IServiceCollection service, EventHubOptions options)
    {
        if (options.EnableMetrics)
        {
            service.TryAddSingleton<IEventHub>(sp =>
                new EventHubChannel(sp.GetRequiredService<EventHubMetrics>(), sp.GetRequiredService<IOptions<EventHubOptions>>(), sp.GetRequiredService<ILogger<EventHubChannel>>()));
        }
        else
        {
            service.TryAddSingleton<IEventHub>(sp =>
                new EventHubChannel(sp.GetRequiredService<IOptions<EventHubOptions>>(), sp.GetRequiredService<ILogger<EventHubChannel>>()));
        }
        return service;
    }
    private static IServiceCollection AddEventHubCollectionServices(this IServiceCollection service, EventHubOptions options)
    {
        if (options.EnableMetrics)
        {
            service.TryAddSingleton<IEventHub>(sp =>
                new EventHubCollection(sp.GetRequiredService<EventHubMetrics>(), sp.GetRequiredService<IOptions<EventHubOptions>>(), sp.GetRequiredService<ILogger<EventHubCollection>>()));
        }
        else
        {
            service.TryAddSingleton<IEventHub>(sp =>
                new EventHubCollection(sp.GetRequiredService<IOptions<EventHubOptions>>(), sp.GetRequiredService<ILogger<EventHubCollection>>()));
        }
        return service;
    }

    private static IServiceCollection AddTelemetryServices(this IServiceCollection service, EventHubOptions options)
    {
        service.TryAddSingleton<EventHubMetrics>((sp) => new EventHubMetrics(sp.GetRequiredService<IMeterFactory>(), options.MetricsName));

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
        return service;
    }


}

