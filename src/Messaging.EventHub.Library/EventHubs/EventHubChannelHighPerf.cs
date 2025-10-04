using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading.Channels;

namespace Messaging.EventHub.Library.EventHubs;

/// <summary>
/// High-performance channel-based EventHub with:
///  - ImmutableArray-based subscriber lists (allocation-free publish fast-path)
///  - TryWrite-first publisher (no CTS when queues aren't full)
///  - Fan-out to all compatible subscriber types (IsAssignableFrom)
///  - Optional "fire-and-collect" for handler scheduling
///  - Deterministic DrainAsync() for tests/maintenance
/// </summary>
public sealed class EventHubChannelHighPerf : IEventHub
{
    private readonly ILogger<EventHubChannelHighPerf> logger;
    private readonly EventHubOptions options;
    private readonly EventHubMetrics? metrics;

    // Toggle: when true, handlers are fired and collected; channel keeps draining under load.
    // Keep false for strict per-message completion semantics (better for tests).
    private readonly bool fireAndCollectHandlers = false;

    // Signal-only channel
    private readonly Channel<string> eventChannel;

    // Data channels organized by eventName -> subscriber type (T)
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<Type, IChannelWrapper>> dataChannelsByEvent = new(StringComparer.Ordinal);

    // Signal-only subscribers: eventName -> ImmutableArray<handler>
    private readonly ConcurrentDictionary<string, ImmutableArray<Func<CancellationToken, Task>>> eventSubscribers = new(StringComparer.Ordinal);

    // "Subscribe to all" (signal notifications across all events): ImmutableArray<handler>
    private ImmutableArray<Func<string, CancellationToken, Task>> allEventsSubscribers = ImmutableArray<Func<string, CancellationToken, Task>>.Empty;

    private readonly CancellationTokenSource shutdownTokenSource = new();
    private readonly Task signalProcessingTask;
    private volatile bool isDisposed;

    public EventHubChannelHighPerf(IOptions<EventHubOptions> options, ILogger<EventHubChannelHighPerf> logger)
    {
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this.options = (options ?? throw new ArgumentNullException(nameof(options))).Value;
        this.fireAndCollectHandlers = this.options.FireAndCollectHandlers;

        eventChannel = CreateChannel<string>();
        signalProcessingTask = Task.Run(() => ProcessSignalsAsync(shutdownTokenSource.Token));
    }

    public EventHubChannelHighPerf(EventHubMetrics metrics, IOptions<EventHubOptions> options, ILogger<EventHubChannelHighPerf> logger)
        : this(options, logger)
    {
        this.metrics = metrics ?? throw new ArgumentNullException(nameof(metrics));
    }

    // ------------------- Public API -------------------

    // Subscribe to signal-only events

    public IDisposable Subscribe(string eventName, Func<CancellationToken, Task> handler)
    {
        ThrowIfDisposed();
        ArgumentException.ThrowIfNullOrWhiteSpace(eventName);
        ArgumentNullException.ThrowIfNull(handler);

        // Atomically add the handler (duplicates allowed)
        eventSubscribers.AddOrUpdate(
            eventName,
            static (_, h) => ImmutableArray.Create(h),                 // first add
            static (_, cur, h) => cur.Add(h),                          // append
            handler);

        UpdateSubscriberMetrics();

        return new UnSubscriber(() =>
        {
            // Remove exactly ONE occurrence of this handler
            while (true)
            {
                if (!eventSubscribers.TryGetValue(eventName, out var cur))
                    break;

                var idx = cur.IndexOf(handler);
                if (idx < 0)
                    break; // nothing to remove

                var next = cur.RemoveAt(idx);

                if (next.IsEmpty)
                {
                    // Try to remove the whole key when it becomes empty
                    if (eventSubscribers.TryRemove(eventName, out var removed))
                    {
                        // if someone raced and re-added, reinsert their value
                        if (!removed.IsEmpty)
                            eventSubscribers.TryAdd(eventName, removed);
                        break;
                    }
                    // If TryRemove failed because value changed, loop and retry
                }
                else
                {
                    if (eventSubscribers.TryUpdate(eventName, next, cur))
                        break; // success
                               // else: value changed, retry
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

        var wrappersForEvent = dataChannelsByEvent.GetOrAdd(eventName, static _ => new ConcurrentDictionary<Type, IChannelWrapper>());

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

    // Subscribe to all signal notifications
    public IDisposable SubscribeToAll(Func<string, CancellationToken, Task> handler)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(handler);

        ImmutableInterlocked.Update(ref allEventsSubscribers, arr => arr.Add(handler));
        UpdateSubscriberMetrics();

        return new UnSubscriber(() =>
        {
            // Remove only one instance
            while (true)
            {
                var cur = allEventsSubscribers;
                var idx = cur.IndexOf(handler);
                if (idx < 0) break;
                var next = cur.RemoveAt(idx);
                if (ImmutableInterlocked.InterlockedCompareExchange(ref allEventsSubscribers, next, cur) == cur)
                    break;
            }
            UpdateSubscriberMetrics();
        });
    }

    // Publish signal-only event (TryWrite-first)
    public async Task Publish(string eventName, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ArgumentException.ThrowIfNullOrWhiteSpace(eventName);

        var writer = eventChannel.Writer;
        if (!writer.TryWrite(eventName))
        {
            var wait = writer.WaitToWriteAsync(cancellationToken);
            if (!wait.IsCompletedSuccessfully)
            {
                CancellationToken ct = cancellationToken;
                using var cts = options.MaxChannelCapacity.HasValue
                    ? CancellationTokenSource.CreateLinkedTokenSource(cancellationToken)
                    : null;
                if (cts != null)
                {
                    cts.CancelAfter(options.BackpressureTimeout);
                    ct = cts.Token;
                }

                if (!await wait.AsTask().WaitAsync(ct).ConfigureAwait(false))
                    throw new TimeoutException($"Failed to publish signal event '{eventName}' within timeout period");
            }

            if (!writer.TryWrite(eventName))
                throw new TimeoutException($"Failed to publish signal event '{eventName}' after wait");
        }

        metrics?.IncrementEventsPublished(eventName);
    }

    public Task Publish<T>(T data, CancellationToken cancellationToken = default) => Publish(typeof(T).FullName!, data, cancellationToken);

    public Task Publish(object data, CancellationToken cancellationToken = default) => Publish(data.GetType().FullName!, data, cancellationToken);

    // Publish data with fan-out to all compatible subscriber types (IsAssignableFrom); TryWrite-first
    public async Task Publish<T>(string eventName, T data, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ArgumentException.ThrowIfNullOrWhiteSpace(eventName);
        ArgumentNullException.ThrowIfNull(data);

        var runtimeType = data.GetType();

        if (!dataChannelsByEvent.TryGetValue(eventName, out var wrappersForEvent) || wrappersForEvent.IsEmpty)
            return;

        // Build target list without LINQ allocations
        List<IChannelWrapper>? tmp = null;
        foreach (var kvp in wrappersForEvent)
        {
            if (kvp.Key.IsAssignableFrom(runtimeType))
            {
                (tmp = tmp ?? new List<IChannelWrapper>(4)).Add(kvp.Value);
            }
        }
        if (tmp is null) return;

        // Publish to each target with TryWrite-first
        List<Task>? waits = null;
        foreach (var wrapper in tmp)
        {
            var pub = ((IDataPublisher)wrapper).PublishObjectTryWriteFirst(data!, options, cancellationToken);
            if (pub is not null)
            {
                (waits = waits ?? new List<Task>(4)).Add(pub);
            }
        }

        if (waits is not null)
            await Task.WhenAll(waits).ConfigureAwait(false);

        if (metrics != null)
        {
            var label = $"{eventName}:{runtimeType.FullName}";
            metrics.IncrementEventsPublished(label);
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

    /// <summary>
    /// Deterministically waits until:
    ///  1) all in-flight data items (in all wrappers) are processed, then
    ///  2) the signal pipeline has drained up to a barrier signal.
    /// </summary>
    public async Task DrainAsync(CancellationToken ct = default)
    {
        ThrowIfDisposed();

        // 1) Wait for all data wrappers (snapshot) to go idle
        var wrappersSnapshot = dataChannelsByEvent.Values
            .SelectMany(inner => inner.Values)
            .Distinct()
            .ToArray();

        var idleTasks = wrappersSnapshot.Select(w => w.WaitForIdleAsync(ct));
        await Task.WhenAll(idleTasks).ConfigureAwait(false);

        // 2) Drain the signal pipeline with a barrier
        var barrier = $"__drain__:{Guid.NewGuid():N}";
        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        using var sub = SubscribeToAll(async (name, token) =>
        {
            if (name == barrier)
                tcs.TrySetResult();
            await Task.CompletedTask;
        });

        await Publish(barrier, ct).ConfigureAwait(false);

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(TimeSpan.FromSeconds(10)); // safety timeout
        await tcs.Task.WaitAsync(cts.Token).ConfigureAwait(false);
    }

    // --------------- Background processing (signals) ---------------

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
            // expected during shutdown
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
            eventSubscribers.TryGetValue(eventName, out var subsArr);
            var allArr = allEventsSubscribers;

            var subsLen = subsArr.IsDefaultOrEmpty ? 0 : subsArr.Length;
            var allLen = allArr.Length;
            var total = subsLen + allLen;

            if (total == 0)
                return;

            if (total == 1)
            {
                if (subsLen == 1) await SafeInvokeAsync(eventName, subsArr[0], ct).ConfigureAwait(false);
                else await SafeInvokeAsync(eventName, () => allArr[0](eventName, ct)).ConfigureAwait(false);
            }
            else
            {
                Task[] tasks = new Task[total];
                var idx = 0;

                if (subsLen > 0)
                {
                    var span = subsArr.AsSpan();
                    for (int i = 0; i < span.Length; i++)
                        tasks[idx++] = SafeInvokeAsync(eventName, span[i], ct);
                }
                if (allLen > 0)
                {
                    var spanAll = allArr.AsSpan();
                    for (int i = 0; i < spanAll.Length; i++)
                    {
                        var h = spanAll[i];
                        tasks[idx++] = SafeInvokeAsync(eventName, () => h(eventName, ct));
                    }
                }

                if (fireAndCollectHandlers)
                    _ = Task.WhenAll(tasks).ContinueWith(_ => { }, TaskScheduler.Default);
                else
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

    // Called by data wrappers to notify all-events subscribers for DATA events as well
    private async Task NotifyAllSubscribersAsync(string eventName, CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            var allArr = allEventsSubscribers;
            if (allArr.Length == 0) return;

            if (allArr.Length == 1)
            {
                await SafeInvokeAsync(eventName, () => allArr[0](eventName, ct)).ConfigureAwait(false);
            }
            else
            {
                Task[] tasks = new Task[allArr.Length];
                var spanAll = allArr.AsSpan();
                for (int i = 0; i < spanAll.Length; i++)
                {
                    var h = spanAll[i];
                    tasks[i] = SafeInvokeAsync(eventName, () => h(eventName, ct));
                }

                if (fireAndCollectHandlers)
                    _ = Task.WhenAll(tasks).ContinueWith(_ => { }, TaskScheduler.Default);
                else
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

        var signalSubs = eventSubscribers.Values.Sum(arr => arr.Length) + allEventsSubscribers.Length;
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
                AllowSynchronousContinuations = true,  // perf
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
                AllowSynchronousContinuations = true   // perf
            };
            return Channel.CreateUnbounded<T>(unboundedOptions);
        }
    }

    private void ThrowIfDisposed()
    {
        if (isDisposed)
            throw new ObjectDisposedException(nameof(EventHubChannelHighPerf));
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
        catch (OperationCanceledException) { /* expected */ }
        catch (Exception ex) when (ex is AggregateException or TimeoutException)
        {
            logger.LogWarning(ex, "Error during signal channel disposal");
        }

        foreach (var inner in dataChannelsByEvent.Values)
        {
            foreach (var wrapper in inner.Values)
                await wrapper.DisposeAsync().ConfigureAwait(false);
        }

        dataChannelsByEvent.Clear();
        eventSubscribers.Clear();
        allEventsSubscribers = ImmutableArray<Func<string, CancellationToken, Task>>.Empty;
        shutdownTokenSource.Dispose();
        metrics?.Dispose();
    }

    // ------------------- Internal plumbing -------------------

    private interface IChannelWrapper : IDisposable, IAsyncDisposable
    {
        int SubscriberCount { get; }
        Task WaitForIdleAsync(CancellationToken ct);
    }

    private interface IDataPublisher
    {
        // TryWrite-first; returns a Task only if it had to wait, otherwise null
        Task? PublishObjectTryWriteFirst(object data, EventHubOptions options, CancellationToken cancellationToken);
    }

    private sealed class DataChannelWrapper<T> : IChannelWrapper, IDataPublisher
    {
        private readonly string eventName;
        private readonly ILogger logger;
        private readonly EventHubOptions options;
        private readonly EventHubMetrics? metrics;
        private readonly Channel<T> channel;

        // ImmutableArray handlers (allows duplicates; removal removes one occurrence)
        private ImmutableArray<Func<T, CancellationToken, Task>> subscribers = ImmutableArray<Func<T, CancellationToken, Task>>.Empty;

        private readonly CancellationTokenSource wrapperTokenSource;
        private readonly Task processingTask;
        private readonly Func<string, CancellationToken, Task> notifyAllAsync;
        private readonly bool fireAndCollect;

        // Idle tracking
        private int pendingCount; // increment on Publish, decrement after processing completes
        private TaskCompletionSource? idleTcs; // set when someone waits while pending > 0

        private volatile bool isDisposed;

        public int SubscriberCount => subscribers.Length;

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
                    AllowSynchronousContinuations = true,
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
                    AllowSynchronousContinuations = true
                };
                return Channel.CreateUnbounded<T>(unboundedOptions);
            }
        }

        public IDisposable AddSubscriber(Func<T, CancellationToken, Task> handler)
        {
            ArgumentNullException.ThrowIfNull(handler);
            ImmutableInterlocked.Update(ref subscribers, arr => arr.Add(handler));

            return new UnSubscriber(() =>
            {
                while (true)
                {
                    var cur = subscribers;
                    var idx = cur.IndexOf(handler);
                    if (idx < 0) break;
                    var next = cur.RemoveAt(idx);
                    if (ImmutableInterlocked.InterlockedCompareExchange(ref subscribers, next, cur) == cur)
                        break;
                }
            });
        }

        // IDataPublisher: TryWrite-first; returns Task only if waiting was needed
        public Task? PublishObjectTryWriteFirst(object data, EventHubOptions options, CancellationToken cancellationToken)
        {
            if (isDisposed) return null;

            var writer = channel.Writer;
            Interlocked.Increment(ref pendingCount);

            if (writer.TryWrite((T)data))
                return null;

            // Fallback path requires waiting. We return a Task to the caller so they can await all waits together.
            return PublishWithWaitAsync((T)data, options, writer, cancellationToken);
        }

        private async Task PublishWithWaitAsync(T data, EventHubOptions options, ChannelWriter<T> writer, CancellationToken cancellationToken)
        {
            try
            {
                var wait = writer.WaitToWriteAsync(cancellationToken);
                if (!wait.IsCompletedSuccessfully)
                {
                    CancellationToken ct = cancellationToken;
                    using var cts = options.MaxChannelCapacity.HasValue
                        ? CancellationTokenSource.CreateLinkedTokenSource(cancellationToken)
                        : null;
                    if (cts != null)
                    {
                        cts.CancelAfter(options.BackpressureTimeout);
                        ct = cts.Token;
                    }

                    if (!await wait.AsTask().WaitAsync(ct).ConfigureAwait(false))
                        throw new TimeoutException("Channel full");
                }

                if (!writer.TryWrite(data))
                    throw new TimeoutException("Channel full after wait");
            }
            catch
            {
                // Undo increment if we failed to enqueue
                Interlocked.Decrement(ref pendingCount);
                throw;
            }
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
                // expected during shutdown
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in data processing loop for type {Type}", typeof(T).FullName);
            }
        }

        private async Task ProcessSingleDataAsync(T data, CancellationToken cancellationToken)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                var subs = subscribers;
                var len = subs.Length;

                if (len == 1)
                {
                    await SafeInvokeHandlerAsync(subs[0], data, cancellationToken).ConfigureAwait(false);
                }
                else if (len > 1)
                {
                    Task[] tasks = new Task[len];
                    var span = subs.AsSpan();
                    for (int i = 0; i < span.Length; i++)
                        tasks[i] = SafeInvokeHandlerAsync(span[i], data, cancellationToken);

                    if (fireAndCollect)
                        _ = Task.WhenAll(tasks).ContinueWith(_ => { }, TaskScheduler.Default);
                    else
                        await Task.WhenAll(tasks).ConfigureAwait(false);
                }

                if (metrics != null)
                {
                    var typeName = (data?.GetType() ?? typeof(T)).FullName!;
                    metrics.IncrementEventsProcessed($"{eventName}:{typeName}");
                }
            }
            finally
            {
                stopwatch.Stop();
                metrics?.RecordProcessingTime(stopwatch.Elapsed.TotalMilliseconds, eventName);

                if (Interlocked.Decrement(ref pendingCount) == 0)
                {
                    var tcs = Interlocked.Exchange(ref idleTcs, null);
                    tcs?.TrySetResult();
                }
            }

            // Notify subscribe-to-all (after handlers)
            await notifyAllAsync(eventName, cancellationToken).ConfigureAwait(false);
        }

        private async Task SafeInvokeHandlerAsync(Func<T, CancellationToken, Task> handler, T data, CancellationToken cancellationToken)
        {
            try
            {
                await handler(data, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (wrapperTokenSource.IsCancellationRequested)
            {
                // expected on shutdown
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in data handler for {Event}", eventName);
                metrics?.IncrementHandlerErrors(eventName);
            }
        }

        public Task WaitForIdleAsync(CancellationToken ct)
        {
            if (Volatile.Read(ref pendingCount) == 0)
                return Task.CompletedTask;

            var tcs = Volatile.Read(ref idleTcs);
            if (tcs == null)
            {
                var newTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                var prev = Interlocked.CompareExchange(ref idleTcs, newTcs, null);
                tcs = prev ?? newTcs;

                // Re-check after installing TCS to avoid races
                if (Volatile.Read(ref pendingCount) == 0)
                {
                    var set = Interlocked.Exchange(ref idleTcs, null);
                    set?.TrySetResult();
                }
            }

            return tcs.Task.WaitAsync(ct);
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
            catch (OperationCanceledException) { /* expected */ }
            catch (Exception ex) when (ex is AggregateException or TimeoutException)
            {
                logger.LogWarning(ex, "Error during data channel disposal for type {Type}", typeof(T).FullName);
            }

            subscribers = ImmutableArray<Func<T, CancellationToken, Task>>.Empty;
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
                unsubscribe();
        }
    }
}
