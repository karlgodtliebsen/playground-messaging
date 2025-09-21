using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading.Channels;

namespace Messaging.EventHub.Library;

/// <summary>
/// An event hub that uses channels for both signal-only events and data-carrying events.
/// Supports asynchronous handlers, generic data events, backpressure handling, and metrics.
/// </summary>
public sealed class EventHub : IEventHub
{
    private readonly ILogger<EventHub> logger;
    private readonly EventHubOptions options;
    private readonly EventHubMetrics? metrics;

    // Signal-only channel (string event names)
    private readonly Channel<string> eventChannel;

    // Per-(eventName,type) data channels
    private readonly ConcurrentDictionary<string, IChannelWrapper> dataChannels = new();

    // Signal-only subscribers: eventName -> set of handlers
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<Func<CancellationToken, Task>, byte>>
        eventSubscribers = new();

    // "Subscribe to all" (signal notifications across all events)
    private readonly ConcurrentDictionary<Func<string, CancellationToken, Task>, byte> allEventsSubscribers = new();

    private readonly CancellationTokenSource shutdownTokenSource = new();
    private readonly Task signalProcessingTask;
    private volatile bool isDisposed;

    public EventHub(ILogger<EventHub> logger, IOptions<EventHubOptions> options)
    {
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this.options = options?.Value ?? throw new ArgumentNullException(nameof(options));

        if (this.options.EnableMetrics)
        {
            metrics = new EventHubMetrics();
        }

        eventChannel = CreateChannel<string>();
        // Process signal-only events (Publish(string ...))
        signalProcessingTask = Task.Run(() => ProcessSignalsAsync(shutdownTokenSource.Token));
    }

    // Subscribe to signal-only events
    public IDisposable Subscribe(string eventName, Func<CancellationToken, Task> handler)
    {
        ThrowIfDisposed();
        ArgumentException.ThrowIfNullOrWhiteSpace(eventName);
        ArgumentNullException.ThrowIfNull(handler);

        var subscribers = eventSubscribers.GetOrAdd(eventName, _ => new ConcurrentDictionary<Func<CancellationToken, Task>, byte>());

        subscribers.TryAdd(handler, 0);
        UpdateSubscriberMetrics();

        return new UnSubscriber(() =>
        {
            if (eventSubscribers.TryGetValue(eventName, out var subs))
            {
                subs.TryRemove(handler, out _);
                if (subs.IsEmpty)
                {
                    eventSubscribers.TryRemove(eventName, out _);
                }
            }
            UpdateSubscriberMetrics();
        });
    }

    // Subscribe to data events (default event name: typeof(T).FullName)
    public IDisposable Subscribe<T>(Func<T, CancellationToken, Task> handler) => Subscribe(typeof(T).FullName!, handler);

    // Subscribe to data events with custom event name
    public IDisposable Subscribe<T>(string eventName, Func<T, CancellationToken, Task> handler)
    {
        ThrowIfDisposed();
        ArgumentException.ThrowIfNullOrWhiteSpace(eventName);
        ArgumentNullException.ThrowIfNull(handler);

        var key = GetEventKey<T>(eventName);
        //var key = GetEventKey(eventName, data);

        if (logger.IsEnabled(LogLevel.Trace))
        {
            logger.LogTrace("Created Subscriber for event '{EventName}' with data type {DataType} and Key: {Key}", eventName, typeof(T).FullName, key);
        }

        var wrapper = GetOrCreateDataChannelWrapper<T>(key, eventName);

        var unSubscriber = wrapper.AddSubscriber(handler);
        UpdateSubscriberMetrics();

        return new UnSubscriber(() =>
        {
            unSubscriber.Dispose();
            UpdateSubscriberMetrics();

            // Clean up empty channels
            if (wrapper.SubscriberCount == 0)
            {
                if (dataChannels.TryRemove(key, out var removedWrapper))
                {
                    removedWrapper.Dispose();
                    UpdateChannelMetrics();
                }
            }
        });
    }

    // Subscribe to all signal notifications (receives event name)
    public IDisposable SubscribeToAll(Func<string, CancellationToken, Task> handler)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(handler);

        allEventsSubscribers.TryAdd(handler, 0);
        UpdateSubscriberMetrics();

        return new UnSubscriber(() =>
        {
            allEventsSubscribers.TryRemove(handler, out _);
            UpdateSubscriberMetrics();
        });
    }

    // Publish signal-only event (bounded if MaxChannelCapacity set; waits up to BackpressureTimeout)
    public async Task Publish(string eventName, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ArgumentException.ThrowIfNullOrWhiteSpace(eventName);

        try
        {
            using var timeoutCts = options.MaxChannelCapacity.HasValue
                ? CancellationTokenSource.CreateLinkedTokenSource(cancellationToken)
                : null;

            if (timeoutCts != null)
            {
                timeoutCts.CancelAfter(options.BackpressureTimeout);
            }

            await eventChannel.Writer.WriteAsync(eventName, timeoutCts?.Token ?? cancellationToken)
                .ConfigureAwait(false);

            metrics?.IncrementEventsPublished(eventName);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning("Backpressure timeout exceeded for signal event '{EventName}'", eventName);
            throw new TimeoutException($"Failed to publish signal event '{eventName}' within timeout period");
        }
    }

    // Publish data event (default event name: typeof(T).FullName)
    public Task Publish<T>(T data, CancellationToken cancellationToken = default)
        => Publish(typeof(T).FullName!, data, cancellationToken);

    public Task Publish(object data, CancellationToken cancellationToken = default)
        => Publish(data.GetType().FullName!, data, cancellationToken);

    // Publish data event with custom event name
    public async Task Publish<T>(string eventName, T data, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ArgumentException.ThrowIfNullOrWhiteSpace(eventName);
        ArgumentNullException.ThrowIfNull(data);

        //var key = GetEventKey<T>(eventName);
        var key = GetEventKey(eventName, data);
        var typeName = data.GetType().FullName!;
        if (logger.IsEnabled(LogLevel.Trace))
        {
            logger.LogTrace("Publishing event '{EventName}' with data type {DataType} and Key: {Key}", eventName, typeName, key);
        }
        if (dataChannels.TryGetValue(key, out var wrapper))
        {
            try
            {
                await ((DataChannelWrapper<T>)wrapper).Publish(data, cancellationToken).ConfigureAwait(false);
                metrics?.IncrementEventsPublished($"{eventName}:{typeof(T).FullName}");
                // NOTE: subscribe-to-all for DATA events is triggered in the wrapper after handler fan-out
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                logger.LogWarning("Backpressure timeout exceeded for data event '{EventName}' with type {DataType} and Key: {Key}", eventName, typeName, key);
                throw new TimeoutException($"Failed to publish data event '{eventName}' within timeout period");
            }
        }
        else
        if (logger.IsEnabled(LogLevel.Debug))
        {
            logger.LogDebug("No subscribers for event: '{EventName}' with data type {DataType} and Key: {Key}", eventName, typeName, key);
        }
    }


    // Non-blocking best-effort publish for signal-only events
    public bool TryPublish(string eventName)
    {
        ThrowIfDisposed();
        ArgumentException.ThrowIfNullOrWhiteSpace(eventName);

        var ok = eventChannel.Writer.TryWrite(eventName);
        if (ok) metrics?.IncrementEventsPublished(eventName);
        return ok;
    }

    private async Task ProcessSignalsAsync(CancellationToken cancellationToken)
    {
        try
        {
            await foreach (var eventName in eventChannel.Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
            {
                await ProcessSignalAsync(eventName, cancellationToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Expected during shutdown
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in signal processing loop");
        }
    }

    private async Task ProcessSignalAsync(string eventName, CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            var subs = eventSubscribers.TryGetValue(eventName, out var d) ? d.Keys.ToArray() : [];

            var all = allEventsSubscribers.Keys.ToArray();

            var tasks = new Task[subs.Length + all.Length];
            var idx = 0;

            for (int i = 0; i < subs.Length; i++)
                tasks[idx++] = SafeInvokeAsync(eventName, subs[i], ct);

            for (int i = 0; i < all.Length; i++)
            {
                var h = all[i];
                tasks[idx++] = SafeInvokeAsync(eventName, () => h(eventName, ct));
            }

            if (tasks.Length > 0) await Task.WhenAll(tasks).ConfigureAwait(false);
            metrics?.IncrementEventsProcessed(eventName);
        }
        finally
        {
            sw.Stop();
            metrics?.RecordProcessingTime(sw.Elapsed.TotalMilliseconds, eventName);
        }
    }

    // Called by data wrappers to notify all-events subscribers for DATA events as well
    private async Task NotifyAllSubscribersAsync(string eventName, CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            var all = allEventsSubscribers.Keys.ToArray();
            if (all.Length == 0) return;

            var tasks = new Task[all.Length];
            for (int i = 0; i < all.Length; i++)
            {
                var h = all[i];
                tasks[i] = SafeInvokeAsync(eventName, () => h(eventName, ct));
            }

            await Task.WhenAll(tasks).ConfigureAwait(false);
            metrics?.IncrementEventsProcessed(eventName);
        }
        finally
        {
            sw.Stop();
            metrics?.RecordProcessingTime(sw.Elapsed.TotalMilliseconds, eventName);
        }
    }

    private Task SafeInvokeAsync(string eventName, Func<CancellationToken, Task> handler, CancellationToken ct)
        => SafeGuard(async () => await handler(ct).ConfigureAwait(false), eventName);

    private Task SafeInvokeAsync(string eventName, Func<Task> handler)
        => SafeGuard(handler, eventName);

    private async Task SafeGuard(Func<Task> run, string eventName)
    {
        try { await run().ConfigureAwait(false); }
        catch (OperationCanceledException) when (shutdownTokenSource.IsCancellationRequested)
        {
            if (logger.IsEnabled(LogLevel.Debug)) logger.LogDebug("Handler canceled for {Event}", eventName);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Handler error for {Event}", eventName);
            metrics?.IncrementHandlerErrors(eventName);
        }
    }

    private DataChannelWrapper<T> GetOrCreateDataChannelWrapper<T>(string key, string eventName)
    {
        if (dataChannels.TryGetValue(key, out var existing))
        {
            return (DataChannelWrapper<T>)existing;
        }

        var wrapper = new DataChannelWrapper<T>(eventName, logger, options, metrics, shutdownTokenSource.Token, NotifyAllSubscribersAsync);

        if (dataChannels.TryAdd(key, wrapper))
        {
            UpdateChannelMetrics();
            return wrapper;
        }

        // Another thread added it first
        wrapper.Dispose();
        return (DataChannelWrapper<T>)dataChannels[key];
    }

    private void UpdateSubscriberMetrics()
    {
        if (metrics == null) return;

        var count = eventSubscribers.Values.Sum(dict => dict.Count) +
                    allEventsSubscribers.Count +
                    dataChannels.Values.Sum(wrapper => wrapper.SubscriberCount);

        metrics.SetSubscriberCount(count);
    }

    private void UpdateChannelMetrics()
    {
        metrics?.SetChannelCount(dataChannels.Count);
    }

    private Channel<T> CreateChannel<T>()
    {
        if (options.MaxChannelCapacity.HasValue)
        {
            var boundedOptions = new BoundedChannelOptions(options.MaxChannelCapacity.Value)
            {
                SingleReader = true,
                SingleWriter = false,
                AllowSynchronousContinuations = false,
                FullMode = options.FullMode
            };
            return Channel.CreateBounded<T>(boundedOptions);
        }
        else
        {
            var unboundedOptions = new UnboundedChannelOptions
            {
                SingleReader = true,
                SingleWriter = false,
                AllowSynchronousContinuations = false
            };
            return Channel.CreateUnbounded<T>(unboundedOptions);
        }
    }

    private static string GetEventKey(string eventName, object data) => $"{eventName}:{data.GetType().FullName}";
    private static string GetEventKey<T>(string eventName) => $"{eventName}:{typeof(T).FullName}";

    private void ThrowIfDisposed()
    {
        if (isDisposed)
        {
            throw new ObjectDisposedException(nameof(EventHub));
        }
    }

    public void Dispose() => DisposeAsync().AsTask().GetAwaiter().GetResult();

    public async ValueTask DisposeAsync()
    {
        if (isDisposed) return;

        isDisposed = true;

        eventChannel.Writer.TryComplete();
        await shutdownTokenSource.CancelAsync().ConfigureAwait(false);
        try
        {
            await signalProcessingTask.ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            /* expected */
        }
        catch (Exception ex) when (ex is AggregateException or TimeoutException)
        {
            // Log but don't throw during disposal
            logger.LogWarning(ex, "Error during signal channel disposal");
        }

        foreach (var wrapper in dataChannels.Values)
        {
            await wrapper.DisposeAsync().ConfigureAwait(false);
        }

        dataChannels.Clear();
        eventSubscribers.Clear();
        allEventsSubscribers.Clear();
        shutdownTokenSource.Dispose();
        metrics?.Dispose();
    }

    // Internal interface for type-erased channel wrappers
    private interface IChannelWrapper : IDisposable, IAsyncDisposable
    {
        int SubscriberCount { get; }
    }

    private sealed class DataChannelWrapper<T> : IChannelWrapper
    {
        private readonly string eventName;
        private readonly ILogger logger;
        private readonly EventHubOptions options;
        private readonly EventHubMetrics? metrics;
        private readonly Channel<T> channel;
        private readonly ConcurrentDictionary<Func<T, CancellationToken, Task>, byte> subscribers = new();
        private readonly CancellationTokenSource wrapperTokenSource;
        private readonly Task processingTask;
        private readonly Func<string, CancellationToken, Task> notifyAllAsync; // <--

        private volatile bool isDisposed;

        public int SubscriberCount => subscribers.Count;

        public DataChannelWrapper(
            string eventName,
            ILogger logger,
            EventHubOptions options,
            EventHubMetrics? metrics,
            CancellationToken parentToken,
            Func<string, CancellationToken, Task> notifyAllAsync)
        {
            this.eventName = eventName ?? throw new ArgumentNullException(nameof(eventName));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.options = options ?? throw new ArgumentNullException(nameof(options));
            this.metrics = metrics;
            this.notifyAllAsync = notifyAllAsync ?? throw new ArgumentNullException(nameof(notifyAllAsync));

            wrapperTokenSource = CancellationTokenSource.CreateLinkedTokenSource(parentToken);
            channel = CreateChannel();
            processingTask = Task.Run(() => ProcessDataAsync(wrapperTokenSource.Token));
        }

        private Channel<T> CreateChannel()
        {
            if (options.MaxChannelCapacity.HasValue)
            {
                var boundedOptions = new BoundedChannelOptions(options.MaxChannelCapacity.Value)
                {
                    SingleReader = true,
                    SingleWriter = false,
                    AllowSynchronousContinuations = false,
                    FullMode = options.FullMode
                };
                return Channel.CreateBounded<T>(boundedOptions);
            }
            else
            {
                var unboundedOptions = new UnboundedChannelOptions
                {
                    SingleReader = true,
                    SingleWriter = false,
                    AllowSynchronousContinuations = false
                };
                return Channel.CreateUnbounded<T>(unboundedOptions);
            }
        }

        public IDisposable AddSubscriber(Func<T, CancellationToken, Task> handler)
        {
            ArgumentNullException.ThrowIfNull(handler);
            subscribers.TryAdd(handler, 0);
            return new UnSubscriber(() => subscribers.TryRemove(handler, out _));
        }

        public async Task Publish(T data, CancellationToken cancellationToken)
        {
            if (isDisposed) return;

            using var timeoutCts = options.MaxChannelCapacity.HasValue
                ? CancellationTokenSource.CreateLinkedTokenSource(cancellationToken)
                : null;

            if (timeoutCts != null)
            {
                timeoutCts.CancelAfter(options.BackpressureTimeout);
            }

            await channel.Writer.WriteAsync(data, timeoutCts?.Token ?? cancellationToken).ConfigureAwait(false);
        }


        private async Task ProcessDataAsync(CancellationToken cancellationToken)
        {
            try
            {
                await foreach (var data in channel.Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
                {
                    await ProcessSingleDataAsync(data, cancellationToken).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // Expected during shutdown
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in data processing loop for type {Type}", typeof(T).FullName);
            }
        }

        private async Task ProcessSingleDataAsync(T data, CancellationToken cancellationToken)
        {
            var stopwatch = Stopwatch.StartNew();
            var label = $"{eventName}:{typeof(T).FullName}";
            try
            {
                var subs = subscribers.Keys.ToArray();
                var tasks = new Task[subs.Length];

                for (int i = 0; i < subs.Length; i++)
                {
                    var handler = subs[i];
                    tasks[i] = SafeInvokeHandlerAsync(handler, data, cancellationToken, label);
                }

                if (tasks.Length > 0) await Task.WhenAll(tasks).ConfigureAwait(false);
                metrics?.IncrementEventsProcessed(label);
            }
            finally
            {
                stopwatch.Stop();
                metrics?.RecordProcessingTime(stopwatch.Elapsed.TotalMilliseconds, label);
            }

            // After data handlers, notify "subscribe-to-all" listeners (by event name)
            await notifyAllAsync(eventName, cancellationToken).ConfigureAwait(false);
        }

        private async Task SafeInvokeHandlerAsync(
            Func<T, CancellationToken, Task> handler,
            T data,
            CancellationToken cancellationToken,
            string label)
        {
            try
            {
                await handler(data, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (wrapperTokenSource.IsCancellationRequested)
            {
                logger.LogDebug("Data handler canceled for {Label}", label);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in data handler for {Label}", label);
                metrics?.IncrementHandlerErrors(label);
            }
        }

        public void Dispose() => DisposeAsync().AsTask().GetAwaiter().GetResult();

        public async ValueTask DisposeAsync()
        {
            if (isDisposed) return;
            isDisposed = true;

            channel.Writer.TryComplete();
            await wrapperTokenSource.CancelAsync().ConfigureAwait(false);
            try
            {
                await processingTask.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                /* expected */
            }
            catch (Exception ex) when (ex is AggregateException or TimeoutException)
            {
                // Log but don't throw during disposal
                logger.LogWarning(ex, "Error during data channel disposal for type {Type}", typeof(T).FullName);
            }

            subscribers.Clear();
            wrapperTokenSource.Dispose();
        }
    }

    private sealed class UnSubscriber(Action unsubscribe) : IDisposable
    {
        private readonly Action unsubscribe = unsubscribe ?? throw new ArgumentNullException(nameof(unsubscribe));
        private int isDisposed;

        public void Dispose()
        {
            if (Interlocked.Exchange(ref isDisposed, 1) == 0)
            {
                unsubscribe();
            }
        }
    }
}
