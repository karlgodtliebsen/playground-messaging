using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading.Channels;

namespace Messaging.EventHub.Library.EventHubs;

/// <summary>
/// An event hub that uses channels for both signal-only events and data-carrying events.
/// Supports asynchronous handlers, generic data events, backpressure handling, and metrics.
/// Features:
///  - Multiple registrations of the same handler (using Guid keys)
///  - Multiple data subscribers per event, including base/interface subscribers via IsAssignableFrom
///  - Optional "fire-and-collect" mode to avoid blocking channel drains on slow handlers (signals + data)
/// </summary>
public sealed class EventHubChannel : IEventHub
{
    private readonly ILogger<EventHubChannel> logger;
    private readonly EventHubOptions options;
    private readonly EventHubMetrics? metrics;

    // --- Optional improvement toggle ---
    // When true, per-message handler invocations are fired and collected in the background
    // so the channel can continue draining under load.
    // Keep false to preserve strict per-message completion semantics.
    private readonly bool fireAndCollectHandlers = false;

    // Signal-only channel (string event names)
    private readonly Channel<string> eventChannel;

    // Data channels organized by event name -> subscriber type (T)
    // Each (eventName, T) holds a channel wrapper that fans out to N handlers of T.
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<Type, IChannelWrapper>> dataChannelsByEvent =
        new(StringComparer.Ordinal);

    // Signal-only subscribers: eventName -> map(Guid -> handler)
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<Guid, Func<CancellationToken, Task>>>
        eventSubscribers = new();

    // "Subscribe to all" (signal notifications across all events): map(Guid -> handler)
    private readonly ConcurrentDictionary<Guid, Func<string, CancellationToken, Task>> allEventsSubscribers = new();

    private readonly CancellationTokenSource shutdownTokenSource = new();
    private readonly Task signalProcessingTask;
    private volatile bool isDisposed;

    public EventHubChannel(IOptions<EventHubOptions> options, ILogger<EventHubChannel> logger)
    {
        this.logger = logger;
        this.options = options.Value;
        eventChannel = CreateChannel<string>();
        signalProcessingTask = Task.Run(() => ProcessSignalsAsync(shutdownTokenSource.Token));
    }

    public EventHubChannel(EventHubMetrics metrics, IOptions<EventHubOptions> options, ILogger<EventHubChannel> logger) : this(options, logger)
    {
        this.metrics = metrics;
    }

    // Subscribe to signal-only events
    public IDisposable Subscribe(string eventName, Func<CancellationToken, Task> handler)
    {
        ThrowIfDisposed();
        ArgumentException.ThrowIfNullOrWhiteSpace(eventName);
        ArgumentNullException.ThrowIfNull(handler);

        var subscribers = eventSubscribers.GetOrAdd(eventName,
            _ => new ConcurrentDictionary<Guid, Func<CancellationToken, Task>>());

        var id = Guid.NewGuid();
        subscribers.TryAdd(id, handler);
        UpdateSubscriberMetrics();

        return new UnSubscriber(() =>
        {
            if (eventSubscribers.TryGetValue(eventName, out var subs))
            {
                subs.TryRemove(id, out _);
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

        if (logger.IsEnabled(LogLevel.Trace))
        {
            logger.LogTrace("Created Subscriber for event '{EventName}' with data type {DataType}", eventName, typeof(T).FullName);


        }

        var wrappersForEvent = dataChannelsByEvent.GetOrAdd(
            eventName,
            static _ => new ConcurrentDictionary<Type, IChannelWrapper>());

        var wrapper = (DataChannelWrapper<T>)wrappersForEvent.GetOrAdd(
            typeof(T),
            static (_, state) =>
            {
                var (evt, logger, options, metrics, cts, notifyAll, fireAndCollect) = state;
                return new DataChannelWrapper<T>(evt, logger, options, metrics, cts.Token, notifyAll, fireAndCollect);
            },
            (eventName, logger, options, metrics, shutdownTokenSource, (Func<string, CancellationToken, Task>)NotifyAllSubscribersAsync, fireAndCollectHandlers));

        var unSubscriber = wrapper.AddSubscriber(handler);
        UpdateSubscriberMetrics();
        UpdateChannelMetrics();

        return new UnSubscriber(() =>
        {
            unSubscriber.Dispose();
            UpdateSubscriberMetrics();

            // Clean up empty wrapper
            if (wrapper.SubscriberCount == 0)
            {
                if (wrappersForEvent.TryRemove(typeof(T), out var removed))
                {
                    removed.Dispose();
                    if (wrappersForEvent.IsEmpty)
                        dataChannelsByEvent.TryRemove(eventName, out _);

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

        var id = Guid.NewGuid();
        allEventsSubscribers.TryAdd(id, handler);
        UpdateSubscriberMetrics();

        return new UnSubscriber(() =>
        {
            allEventsSubscribers.TryRemove(id, out _);
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
        catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning(ex, "Backpressure timeout exceeded for signal event '{EventName}'", eventName);
            throw new TimeoutException($"Failed to publish signal event '{eventName}' within timeout period");
        }
    }

    public Task Publish<T>(T data, CancellationToken cancellationToken = default)
        => Publish(typeof(T).FullName!, data, cancellationToken);

    public Task Publish(object data, CancellationToken cancellationToken = default)
        => Publish(data.GetType().FullName!, data, cancellationToken);

    // Publish data with fan-out to all compatible subscriber types (IsAssignableFrom)
    public async Task Publish<T>(string eventName, T data, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ArgumentException.ThrowIfNullOrWhiteSpace(eventName);
        ArgumentNullException.ThrowIfNull(data);

        var runtimeType = data.GetType();

        if (logger.IsEnabled(LogLevel.Trace))
        {
            logger.LogTrace("Publishing event '{EventName}' with data type {DataType}", eventName, runtimeType.FullName);
        }

        if (!dataChannelsByEvent.TryGetValue(eventName, out var wrappersForEvent) || wrappersForEvent.IsEmpty)
        {
            if (logger.IsEnabled(LogLevel.Debug))
            {
                logger.LogDebug("No subscribers for event: '{EventName}'", eventName);
            }
            return;
        }

        // Select all wrappers where TSubscriber.IsAssignableFrom(runtimeType)
        var targets = wrappersForEvent
            .Where(kvp => kvp.Key.IsAssignableFrom(runtimeType))
            .Select(kvp => kvp.Value)
            .ToArray();

        if (targets.Length == 0)
        {
            if (logger.IsEnabled(LogLevel.Debug))
            {
                logger.LogDebug("No compatible subscribers for event: '{EventName}' and type {Type}", eventName, runtimeType.FullName);
            }
            return;
        }

        var publishTasks = new List<Task>(targets.Length);

        foreach (var wrapper in targets)
        {
            try
            {
                publishTasks.Add(((IDataPublisher)wrapper).PublishObject(data!, cancellationToken));
            }
            catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
            {
                logger.LogWarning(ex, "Backpressure timeout exceeded for data event '{EventName}' with runtime type {Type}", eventName, runtimeType.FullName);
                throw new TimeoutException($"Failed to publish data event '{eventName}' within timeout period");
            }
        }

        // Complete the publish only after all wrappers accepted the message (preserves backpressure semantics)
        await Task.WhenAll(publishTasks).ConfigureAwait(false);

        metrics?.IncrementEventsPublished($"{eventName}:{runtimeType.FullName}");
        // NOTE: subscribe-to-all for DATA events is triggered in the wrapper after handler fan-out
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
            var subs = eventSubscribers.TryGetValue(eventName, out var d)
                ? d.Values.ToArray()
                : [];

            var all = allEventsSubscribers.Values.ToArray();

            var tasks = new Task[subs.Length + all.Length];
            var idx = 0;

            for (int i = 0; i < subs.Length; i++)
                tasks[idx++] = SafeInvokeAsync(eventName, subs[i], ct);

            for (int i = 0; i < all.Length; i++)
            {
                var h = all[i];
                tasks[idx++] = SafeInvokeAsync(eventName, () => h(eventName, ct));
            }

            if (tasks.Length > 0)
            {
                if (fireAndCollectHandlers)
                {
                    _ = Task.WhenAll(tasks).ContinueWith(_ => { /* collected */ }, TaskScheduler.Default);
                }
                else
                {
                    await Task.WhenAll(tasks).ConfigureAwait(false);
                }
            }

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
            var all = allEventsSubscribers.Values.ToArray();
            if (all.Length == 0) return;

            var tasks = new Task[all.Length];
            for (int i = 0; i < all.Length; i++)
            {
                var h = all[i];
                tasks[i] = SafeInvokeAsync(eventName, () => h(eventName, ct));
            }

            if (fireAndCollectHandlers)
            {
                _ = Task.WhenAll(tasks).ContinueWith(_ => { /* collected */ }, TaskScheduler.Default);
            }
            else
            {
                await Task.WhenAll(tasks).ConfigureAwait(false);
            }

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

    private void UpdateSubscriberMetrics()
    {
        if (metrics == null) return;

        var signalSubs = eventSubscribers.Values.Sum(dict => dict.Count) + allEventsSubscribers.Count;
        var dataSubs = dataChannelsByEvent.Values.Sum(inner => inner.Values.Sum(w => w.SubscriberCount));
        metrics.SetSubscriberCount(signalSubs + dataSubs);
    }

    private void UpdateChannelMetrics()
    {
        if (metrics == null) return;

        var channelCount = dataChannelsByEvent.Values.Sum(inner => inner.Count);
        metrics.SetChannelCount(channelCount);
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

    private void ThrowIfDisposed()
    {
        if (isDisposed)
        {
            throw new ObjectDisposedException(nameof(EventHubChannel));
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

        // Dispose all data wrappers
        foreach (var inner in dataChannelsByEvent.Values)
        {
            foreach (var wrapper in inner.Values)
            {
                await wrapper.DisposeAsync().ConfigureAwait(false);
            }
        }

        dataChannelsByEvent.Clear();
        eventSubscribers.Clear();
        allEventsSubscribers.Clear();
        shutdownTokenSource.Dispose();
        metrics?.Dispose();
    }

    // ---------- Internal plumbing ----------

    // Internal interface for type-erased channel wrappers
    private interface IChannelWrapper : IDisposable, IAsyncDisposable
    {
        int SubscriberCount { get; }
    }

    // Used to allow PublishObject without knowing T at compile time
    private interface IDataPublisher
    {
        Task PublishObject(object data, CancellationToken ct);
    }

    private sealed class DataChannelWrapper<T> : IChannelWrapper, IDataPublisher
    {
        private readonly string eventName;
        private readonly ILogger logger;
        private readonly EventHubOptions options;
        private readonly EventHubMetrics? metrics;
        private readonly Channel<T> channel;

        // Use Guid -> handler to allow duplicate handler instances
        private readonly ConcurrentDictionary<Guid, Func<T, CancellationToken, Task>> subscribers = new();

        private readonly CancellationTokenSource wrapperTokenSource;
        private readonly Task processingTask;
        private readonly Func<string, CancellationToken, Task> notifyAllAsync;
        private readonly bool fireAndCollect;

        private volatile bool isDisposed;

        public int SubscriberCount => subscribers.Count;

        public DataChannelWrapper(
            string eventName,
            ILogger logger,
            EventHubOptions options,
            EventHubMetrics? metrics,
            CancellationToken parentToken,
            Func<string, CancellationToken, Task> notifyAllAsync,
            bool fireAndCollect)
        {
            this.eventName = eventName ?? throw new ArgumentNullException(nameof(eventName));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.options = options ?? throw new ArgumentNullException(nameof(options));
            this.metrics = metrics;
            this.notifyAllAsync = notifyAllAsync ?? throw new ArgumentNullException(nameof(notifyAllAsync));
            this.fireAndCollect = fireAndCollect;

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
            var id = Guid.NewGuid();
            subscribers.TryAdd(id, handler);
            return new UnSubscriber(() => subscribers.TryRemove(id, out _));
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

        // IDataPublisher: publish without compile-time T
        public Task PublishObject(object data, CancellationToken ct)
        {
            // This cast is safe by construction: caller only invokes when typeof(T).IsAssignableFrom(data.GetType())
            return Publish((T)data, ct);
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
            var typeName = data is not null ? data.GetType().FullName : typeof(T).FullName;
            var label = $"{eventName}:{typeName}";
            try
            {
                var subs = subscribers.Values.ToArray();
                if (subs.Length > 0)
                {
                    var tasks = new Task[subs.Length];
                    for (int i = 0; i < subs.Length; i++)
                    {
                        var handler = subs[i];
                        tasks[i] = SafeInvokeHandlerAsync(handler, data, label, cancellationToken);
                    }

                    if (fireAndCollect)
                    {
                        _ = Task.WhenAll(tasks).ContinueWith(_ => { /* collected */ }, TaskScheduler.Default);
                    }
                    else
                    {
                        await Task.WhenAll(tasks).ConfigureAwait(false);
                    }
                }

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

        private async Task SafeInvokeHandlerAsync(Func<T, CancellationToken, Task> handler, T data, string label, CancellationToken cancellationToken)
        {
            try
            {
                await handler(data, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (wrapperTokenSource.IsCancellationRequested)
            {
                // No Operation needed
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
                // No Operation needed
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