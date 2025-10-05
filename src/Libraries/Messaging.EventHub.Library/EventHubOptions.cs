using System.Threading.Channels;

namespace Messaging.EventHub.Library;

/// <summary>
/// Configuration options for the EventHub
/// </summary>
public sealed class EventHubOptions
{
    public const string SectionName = "EventHubOptions";

    /// <summary>
    /// Maximum capacity for bounded channels. If null, unbounded channels are used.
    /// </summary>
    public int? MaxChannelCapacity { get; set; }

    /// <summary>
    /// Behavior when channel capacity is exceeded
    /// </summary>
    public BoundedChannelFullMode FullMode { get; set; } = BoundedChannelFullMode.Wait;

    /// <summary>
    /// Timeout for waiting when channels are full
    /// </summary>
    public TimeSpan BackpressureTimeout { get; set; } = TimeSpan.FromSeconds(30);

    // Toggle: when true, handlers are fired and collected; channel keeps draining under load.
    // Keep false for strict per-message completion semantics (better for tests).

    public bool FireAndCollectHandlers { get; set; } = false;

    public bool EnableUseChannel { get; set; } = false;

    /// <summary>
    /// Whether to enable metrics collection
    /// </summary>
    public bool EnableMetrics { get; set; } = false;
    /// <summary>
    /// 
    /// </summary>
    public string MetricsName { get; set; } = "EventHub";

}