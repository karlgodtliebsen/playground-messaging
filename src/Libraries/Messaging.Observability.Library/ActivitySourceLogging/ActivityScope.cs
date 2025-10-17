using System.Diagnostics;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.Logging;

namespace Messaging.Observability.Library.ActivitySourceLogging;

public readonly struct ActivityScope : IDisposable
{
    private readonly Activity? activity;
    private readonly Stopwatch stopwatch;
    private readonly Histogram<double> histogram;
    private readonly ILogger logger;
    private readonly string activityName;
    private readonly string activityDescription;
    private readonly IDictionary<string, string>? tags;

    internal ActivityScope(ActivitySource activitySource, string activityName, string activityDescription, ActivityKind activityKind, IDictionary<string, string>? tags, Histogram<double> histogram, ILogger logger)
    {
        this.histogram = histogram;
        this.logger = logger;
        this.activityName = activityName;
        this.activityDescription = activityDescription;
        this.tags = tags;
        activity = activitySource.StartActivity(activityDescription, activityKind);
        stopwatch = Stopwatch.StartNew();
    }

    public void Dispose()
    {
        stopwatch.Stop();
        var durationMs = stopwatch.Elapsed.TotalMilliseconds;

        try
        {
            // Record metrics
            histogram.Record(durationMs,
                new KeyValuePair<string, object?>("operation", activityName),
                new KeyValuePair<string, object?>("method", activityDescription));

            // Set activity tags
            activity?.SetTag("duration.ms", durationMs);
            activity?.SetTag("operation", activityName);
            activity?.SetTag("activity", this.activityDescription);

            if (tags is not null)
            {
                foreach (var kvp in tags)
                {
                    activity?.SetTag(kvp.Key, kvp.Value);
                }
            }

            // Log completion
            logger.LogInformation("{Operation} - {Description} completed in {DurationMs:F2} ms", activityName, activityDescription, durationMs);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error while recording activity completion for {Operation}", activityName);
        }
        finally
        {
            activity?.Dispose();
        }
    }
}