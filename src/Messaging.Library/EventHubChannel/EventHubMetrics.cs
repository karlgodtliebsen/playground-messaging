using System.Diagnostics.Metrics;

namespace Messaging.Library.EventHubChannel;

/// <summary>
/// Metrics for monitoring EventHub performance
/// </summary>
public sealed class EventHubMetrics : IDisposable
{
    private readonly Meter meter;
    private readonly Counter<long> eventsPublished;
    private readonly Counter<long> eventsProcessed;
    private readonly Counter<long> handlerErrors;
    private readonly ObservableGauge<int> activeSubscribers;
    private readonly ObservableGauge<int> activeChannels;
    private readonly Histogram<double> eventProcessingTime;

    private volatile int totalSubscribers;
    private volatile int totalChannels;

    public EventHubMetrics(string meterName = "EventHub")
    {
        meter = new Meter(meterName);

        eventsPublished = meter.CreateCounter<long>("events_published_total", "count", "Total number of events published");
        eventsProcessed = meter.CreateCounter<long>("events_processed_total", "count", "Total number of events processed");
        handlerErrors = meter.CreateCounter<long>("handler_errors_total", "count", "Total number of handler errors");

        activeSubscribers = meter.CreateObservableGauge("active_subscribers",
            () => totalSubscribers, "count", "Current number of active subscribers");
        activeChannels = meter.CreateObservableGauge("active_channels",
            () => totalChannels, "count", "Current number of active channels");

        eventProcessingTime = meter.CreateHistogram<double>("event_processing_duration", "ms", "Event processing duration in milliseconds");
    }

    public void IncrementEventsPublished(string eventName) => eventsPublished.Add(1, new KeyValuePair<string, object?>("event_name", eventName));
    public void IncrementEventsProcessed(string eventName) => eventsProcessed.Add(1, new KeyValuePair<string, object?>("event_name", eventName));
    public void IncrementHandlerErrors(string eventName) => handlerErrors.Add(1, new KeyValuePair<string, object?>("event_name", eventName));
    public void RecordProcessingTime(double milliseconds, string eventName) => eventProcessingTime.Record(milliseconds, new KeyValuePair<string, object?>("event_name", eventName));

    public void SetSubscriberCount(int count) => totalSubscribers = count;
    public void SetChannelCount(int count) => totalChannels = count;
    public void Dispose() => meter.Dispose();
}