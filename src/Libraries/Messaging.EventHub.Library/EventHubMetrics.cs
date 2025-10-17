using Microsoft.Extensions.Logging;

using System.Diagnostics.Metrics;

namespace Messaging.EventHub.Library;

public sealed class EventHubMetrics : IDisposable
{
    private readonly Meter meter;
    private readonly Counter<long> eventsPublished;
    private readonly Counter<long> eventsProcessed;
    private readonly Counter<long> handlerErrors;
    private readonly ObservableGauge<int> activeSubscribers;
    private readonly ObservableGauge<int> activeChannels;
    private readonly Histogram<double> eventProcessingTime;
    private readonly ILogger<EventHubMetrics>? logger;

    private volatile int totalSubscribers;
    private volatile int totalChannels;
    private readonly bool ownsMeter;

    public EventHubMetrics(IMeterFactory meterFactory, string meterName, ILogger<EventHubMetrics>? logger = null)
    {
        if (meterFactory is null) throw new ArgumentNullException(nameof(meterFactory));

        this.logger = logger;
        meter = meterFactory.Create(meterName);
        ownsMeter = false;

        logger?.LogInformation("EventHubMetrics created with meter: {MeterName}", meterName);

        eventsPublished = meter.CreateCounter<long>(
            "events_published_total",
            "count",
            "Total number of events published");

        eventsProcessed = meter.CreateCounter<long>(
            "events_processed_total",
            "count",
            "Total number of events processed");

        handlerErrors = meter.CreateCounter<long>(
            "handler_errors_total",
            "count",
            "Total number of handler errors");

        activeSubscribers = meter.CreateObservableGauge(
            "active_subscribers",
            () => totalSubscribers,
            "count",
            "Current number of active subscribers");

        activeChannels = meter.CreateObservableGauge(
            "active_channels",
            () => totalChannels,
            "count",
            "Current number of active channels");

        eventProcessingTime = meter.CreateHistogram<double>(
            "event_processing_duration",
            "ms",
            "Event processing duration in milliseconds");

        logger?.LogInformation("EventHubMetrics instruments created: eventsPublished, eventsProcessed, handlerErrors, activeSubscribers, activeChannels, eventProcessingTime");
    }

    public void IncrementEventsPublished(string eventName)
    {
        eventsPublished.Add(1, new KeyValuePair<string, object?>("event_name", eventName));
        logger?.LogDebug("Incremented events_published_total for {EventName}", eventName);
    }

    public void IncrementEventsProcessed(string eventName)
    {
        eventsProcessed.Add(1, new KeyValuePair<string, object?>("event_name", eventName));
        logger?.LogDebug("Incremented events_processed_total for {EventName}", eventName);
    }

    public void IncrementHandlerErrors(string eventName)
    {
        handlerErrors.Add(1, new KeyValuePair<string, object?>("event_name", eventName));
        logger?.LogDebug("Incremented handler_errors_total for {EventName}", eventName);
    }

    public void RecordProcessingTime(double milliseconds, string eventName)
    {
        eventProcessingTime.Record(milliseconds, new KeyValuePair<string, object?>("event_name", eventName));
        logger?.LogDebug("Recorded processing time {Duration}ms for {EventName}", milliseconds, eventName);
    }

    public void SetSubscriberCount(int count)
    {
        totalSubscribers = count;
        logger?.LogDebug("Set subscriber count to {Count}", count);
    }

    public void SetChannelCount(int count)
    {
        totalChannels = count;
        logger?.LogDebug("Set channel count to {Count}", count);
    }

    public void Dispose()
    {
        // If you ever support a 'new Meter(...)' path, only dispose when you own it.
        if (ownsMeter) meter.Dispose();
    }
}