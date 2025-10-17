using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Runtime.CompilerServices;
using Messaging.Observability.Library.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Messaging.Observability.Library.ActivitySourceLogging;

public sealed class ActivitySourceFactory : IActivitySourceFactory, IDisposable
{
    private readonly ILogger<ActivitySourceFactory> logger;

    private readonly Lazy<ActivitySource> activitySourceLazy;
    private readonly Meter meter; // Changed from Lazy - create immediately
    private readonly Lazy<Histogram<double>> durationHistogramLazy;

    public ActivitySourceFactory(IOptions<ApplicationInformationOptions> applicationOptions, ILogger<ActivitySourceFactory> logger)
    {
        this.logger = logger;

        // Create meter immediately so it's registered with IMeterFactory
        meter = new Meter(applicationOptions.Value.ApplicationName, applicationOptions.Value.ApplicationVersion);

        logger.LogInformation("ActivitySourceFactory created meter: {MeterName} version {MeterVersion}",
            meter.Name, applicationOptions.Value.ApplicationVersion);

        activitySourceLazy = new Lazy<ActivitySource>(() =>
            new ActivitySource(applicationOptions.Value.ApplicationName, applicationOptions.Value.ApplicationVersion));

        durationHistogramLazy = new Lazy<Histogram<double>>(() =>
            meter.CreateHistogram<double>(
                name: "operation.duration",
                unit: "ms",
                description: "Operation execution duration in milliseconds"));
    }

    private ActivitySource ActivitySource => activitySourceLazy.Value;
    public Meter Meter => meter; // Return the actual meter, not lazy
    private Histogram<double> DurationHistogram => durationHistogramLazy.Value;

    private bool disposed;
    private IDictionary<string, string>? tags = null;

    public ActivityScope CreateActivity(string activityName, ActivityKind activityKind = ActivityKind.Internal,
        [CallerMemberName] string activityDescription = null!,
        string? activityDescriptionOverwrite = null,
        IDictionary<string, string>? activityTags = null)
    {
        activityDescription = activityDescriptionOverwrite ?? activityDescription ?? "Unknown";
        this.tags = activityTags;
        return disposed
            ? throw new ObjectDisposedException(nameof(ActivitySourceFactory))
            : new ActivityScope(ActivitySource, activityName, activityDescription, activityKind, tags, DurationHistogram, logger);
    }

    public void Dispose()
    {
        if (!disposed)
        {
            // Dispose the meter when factory is disposed
            meter?.Dispose();
            disposed = true;
        }
    }

    /// <summary>
    /// Usage pattern:
    /// <example>
    /// await using var _ = activitySourceLoggerFactory
    /// .CreateActivity()
    /// .WithServiceName(ServiceName).WithActivityKind(ActivityKind.Internal)
    /// .WithActivityDescription("name to overwrite [CallerMemberName] method")
    /// .Start();
    /// </example>
    /// </summary>
    /// <param name="activityDescription"></param>
    /// <returns></returns>
    public ActivitySourceBuilder CreateBuilder([CallerMemberName] string activityDescription = null!)
    {
        var state = new ActivitySourceBuilder(this, activityDescription);
        return state;
    }
}