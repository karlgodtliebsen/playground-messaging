using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Messaging.Observability.Library.Configuration;

public static class ObservabilityConfigurator
{
    public static IServiceCollection AddObservability(this IServiceCollection services, IConfiguration configuration, bool useOtelLoggingProvider = true)
    {
        var appOptions = configuration.GetSection(ApplicationInformationOptions.SectionName).Get<ApplicationInformationOptions>();
        if (appOptions is null)
        {
            appOptions = new ApplicationInformationOptions();
        }

        return services.AddObservability(configuration, appOptions, useOtelLoggingProvider);
    }

    public static IServiceCollection AddObservability(this IServiceCollection services, IConfiguration configuration, ApplicationInformationOptions appOptions, bool useOtelLoggingProvider = true)
    {
        var opts = configuration.GetSection("OpenTelemetry").Get<OtelOptions>() ?? new OtelOptions();
        opts.ServiceName ??= appOptions.ServiceName;
        opts.ServiceVersion ??= appOptions.ApplicationVersion;

        services.AddOpenTelemetry()
            .ConfigureResource(r =>
                r
                    .AddService(serviceName: opts.ServiceName,
                        serviceVersion: opts.ServiceVersion,
                        serviceInstanceId: Environment.MachineName)
                    .ApplyToResource(appOptions)
                    .AddEnvironmentVariableDetector())
            .WithTracing(t =>
            {
                if (!opts.DebugMode)
                {
                    t.SetSampler(new ParentBasedSampler(new TraceIdRatioBasedSampler(opts.SamplingRatio)));
                }
                else
                {
                    t.SetSampler(new ParentBasedSampler(new AlwaysOnSampler()));
                }

                if (opts.AspNetCore)
                    t.AddAspNetCoreInstrumentation(o =>
                    {
                        o.Filter = ctx =>
                        {
                            var p = ctx.Request.Path.Value;
                            return p is not "/health" and not "/metrics";
                        };
                    });
                if (opts.HttpClient) t.AddHttpClientInstrumentation();
                if (opts.SqlClient)
                    t.AddSqlClientInstrumentation(o =>
                    {
                        o.RecordException = true;
                        o.SetDbStatementForText = false;
                    });
                if (opts.Sources.Length > 0) t.AddSource(opts.Sources);
                if (opts.DebugMode) t.AddConsoleExporter();
                if (opts.ExportTraces) t.AddOtlpExporter(e => ConfigureOtlp(e, opts));
            })
            .WithMetrics(m =>
            {
                if (opts.AspNetCore) m.AddAspNetCoreInstrumentation();
                if (opts.HttpClient) m.AddHttpClientInstrumentation();
                if (opts.Runtime) m.AddRuntimeInstrumentation();
                if (opts.Process) m.AddProcessInstrumentation();

                // Add your custom meters
                // if (opts.Meters.Length > 0) m.AddMeter(opts.Meters);

                // Add your custom meters
                if (opts.Meters.Length > 0)
                {
                    foreach (var meter in opts.Meters)
                    {
                        m.AddMeter(meter);
                    }
                }

                // DIAGNOSTIC: Add console exporter to see metrics locally
                if (opts.DebugMode)
                {
                    m.AddConsoleExporter((exporterOptions, metricReaderOptions) =>
                    {
                        metricReaderOptions.PeriodicExportingMetricReaderOptions.ExportIntervalMilliseconds = 5000;
                    });
                }

                // IMPORTANT: Add view configuration for histograms
                m.AddView(
                    instrumentName: "operation.duration",
                    new ExplicitBucketHistogramConfiguration
                    {
                        Boundaries = new double[] { 0, 5, 10, 25, 50, 75, 100, 250, 500, 1000, 2500, 5000, 10000 }
                    });

                m.AddView(
                    instrumentName: "event_processing_duration",
                    new ExplicitBucketHistogramConfiguration
                    {
                        Boundaries = new double[] { 0, 1, 5, 10, 25, 50, 100, 250, 500, 1000 }
                    });

                if (opts.ExportMetrics) m.AddOtlpExporter(e => ConfigureOtlp(e, opts));
            })
            //.WithMetrics(m =>
            //{
            //    if (opts.AspNetCore) m.AddAspNetCoreInstrumentation();
            //    if (opts.HttpClient) m.AddHttpClientInstrumentation();
            //    if (opts.Runtime) m.AddRuntimeInstrumentation();
            //    if (opts.Process) m.AddProcessInstrumentation();
            //    if (opts.Meters.Length > 0) m.AddMeter(opts.Meters);
            //    if (opts.ExportMetrics) m.AddOtlpExporter(e => ConfigureOtlp(e, opts));
            //})
            ;

        // Configure logging separately if using OTel logging provider
        if (useOtelLoggingProvider && opts.ExportLogs)
        {
            services.AddLogging(logging =>
            {
                logging.AddOpenTelemetry(options =>
                {
                    options.SetResourceBuilder(ResourceBuilder.CreateDefault()
                        .AddService(serviceName: opts.ServiceName, serviceVersion: opts.ServiceVersion));
                    options.IncludeScopes = true;
                    options.ParseStateValues = true;
                    options.IncludeFormattedMessage = opts.DebugMode; // Enable for debugging
                    options.AddOtlpExporter(e => ConfigureOtlp(e, opts));
                });
            });
        }

        return services;
    }

    private static void ConfigureOtlp(OtlpExporterOptions e, OtelOptions opts)
    {
        if (!string.IsNullOrWhiteSpace(opts.Endpoint))
        {
            e.Endpoint = new Uri(opts.Endpoint);
        }

        e.Protocol = opts.UseGrpc ? OtlpExportProtocol.Grpc : OtlpExportProtocol.HttpProtobuf;

        // Add headers if needed for debugging
        if (opts.DebugMode) e.Headers = "debug=true";
    }

    // Optional: if you choose the OTel logging provider route
    public static ILoggingBuilder AddOpenTelemetryLogging(this ILoggingBuilder lb) => lb.AddOpenTelemetry(o =>
    {
        o.IncludeScopes = true;
        o.ParseStateValues = true;
        o.IncludeFormattedMessage = false;
        o.AddOtlpExporter();
    });
}