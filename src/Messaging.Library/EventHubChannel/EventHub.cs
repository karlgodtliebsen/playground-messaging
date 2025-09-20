using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using System.Collections.Concurrent;
using System.Threading.Channels;

namespace Messaging.Library.EventHubChannel;

/// <summary>
/// An event hub that uses channels for both signal-only events and data-carrying events.
/// Supports asynchronous handlers, generic data events, backpressure handling, and metrics.
/// </summary>
public sealed class EventHub : IEventHub
{
    private readonly ILogger<EventHub> logger;
    private readonly EventHubOptions options;
    private readonly EventHubMetrics? metrics;
    private readonly Channel<string> signalOnlyChannel;
    private readonly ConcurrentDictionary<string, IChannelWrapper> dataChannels = new();
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<Func<CancellationToken, Task>, byte>> signalOnlySubscribers = new();
    private readonly ConcurrentDictionary<Func<string, CancellationToken, Task>, byte> allSignalSubscribers = new();
    private readonly CancellationTokenSource shutdownTokenSource = new();
    private readonly Task signalProcessingTask;
    private volatile bool isDisposed;

    public EventHub(ILogger<EventHub> logger, IOptions<EventHubOptions> options)
    {
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this.options = options.Value;

        if (this.options.EnableMetrics)
        {
            metrics = new EventHubMetrics();
        }

        signalOnlyChannel = CreateChannel<string>();
        signalProcessingTask = ProcessSignalsAsync(shutdownTokenSource.Token);
    }

    // Subscribe to signal-only events
    public IDisposable Subscribe(string eventName, Func<CancellationToken, Task> handler)
    {
        ThrowIfDisposed();
        ArgumentException.ThrowIfNullOrWhiteSpace(eventName);
        ArgumentNullException.ThrowIfNull(handler);

        var subscribers = signalOnlySubscribers.GetOrAdd(eventName, _ => new ConcurrentDictionary<Func<CancellationToken, Task>, byte>());
        subscribers.TryAdd(handler, 0);

        UpdateSubscriberMetrics();

        return new Unsubscriber(() =>
        {
            if (signalOnlySubscribers.TryGetValue(eventName, out var subs))
            {
                subs.TryRemove(handler, out _);
                if (subs.IsEmpty)
                {
                    signalOnlySubscribers.TryRemove(eventName, out _);
                }
            }
            UpdateSubscriberMetrics();
        });
    }

    // Subscribe to data events (using type name as event name)
    public IDisposable Subscribe<T>(Func<T, CancellationToken, Task> handler)
    {
        return Subscribe(typeof(T).Name, handler);
    }

    // Subscribe to data events with custom event name
    public IDisposable Subscribe<T>(string eventName, Func<T, CancellationToken, Task> handler)
    {
        ThrowIfDisposed();
        ArgumentException.ThrowIfNullOrWhiteSpace(eventName);
        ArgumentNullException.ThrowIfNull(handler);

        var key = GetEventKey<T>(eventName);
        var wrapper = GetOrCreateDataChannelWrapper<T>(key);

        var unsubscriber = wrapper.AddSubscriber(handler);
        UpdateSubscriberMetrics();

        return new Unsubscriber(() =>
        {
            unsubscriber.Dispose();
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

    // Subscribe to all events (receives event name)
    public IDisposable SubscribeToAll(Func<string, CancellationToken, Task> handler)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(handler);

        allSignalSubscribers.TryAdd(handler, 0);
        UpdateSubscriberMetrics();

        return new Unsubscriber(() =>
        {
            allSignalSubscribers.TryRemove(handler, out _);
            UpdateSubscriberMetrics();
        });
    }

    // Publish signal-only event
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

            await signalOnlyChannel.Writer.WriteAsync(eventName, timeoutCts?.Token ?? cancellationToken);
            metrics?.IncrementEventsPublished(eventName);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning("Backpressure timeout exceeded for signal event '{EventName}'", eventName);
            throw new TimeoutException($"Failed to publish signal event '{eventName}' within timeout period");
        }
    }

    // Publish data event (using type name as event name)
    public Task Publish<T>(T data, CancellationToken cancellationToken = default)
    {
        return Publish(typeof(T).Name, data, cancellationToken);
    }

    // Publish data event with custom event name
    public async Task Publish<T>(string eventName, T data, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ArgumentException.ThrowIfNullOrWhiteSpace(eventName);
        ArgumentNullException.ThrowIfNull(data);

        if (logger.IsEnabled(LogLevel.Trace))
        {
            logger.LogTrace("Publishing event '{EventName}' with data type {DataType}", eventName, typeof(T).Name);
        }

        var key = GetEventKey<T>(eventName);
        if (dataChannels.TryGetValue(key, out var wrapper))
        {
            try
            {
                await ((DataChannelWrapper<T>)wrapper).Publish(data, cancellationToken);
                metrics?.IncrementEventsPublished($"{eventName}:{typeof(T).Name}");
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                logger.LogWarning("Backpressure timeout exceeded for data event '{EventName}' with type {DataType}", eventName, typeof(T).Name);
                throw new TimeoutException($"Failed to publish data event '{eventName}' within timeout period");
            }
        }
        else if (logger.IsEnabled(LogLevel.Debug))
        {
            logger.LogDebug("No subscribers for event '{EventName}' with data type {DataType}", eventName, typeof(T).Name);
        }
    }

    private async Task ProcessSignalsAsync(CancellationToken cancellationToken)
    {
        try
        {
            await foreach (var eventName in signalOnlyChannel.Reader.ReadAllAsync(cancellationToken))
            {
                await ProcessSignalAsync(eventName, cancellationToken);
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

    private async Task ProcessSignalAsync(string eventName, CancellationToken cancellationToken)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var tasks = new List<Task>();

        try
        {
            // Process specific event subscribers
            if (signalOnlySubscribers.TryGetValue(eventName, out var subscribers))
            {
                foreach (var handler in subscribers.Keys)
                {
                    tasks.Add(SafeInvokeAsync(handler, cancellationToken, $"subscribe for '{eventName}'"));
                }
            }

            // Process "subscribe all" handlers
            foreach (var handler in allSignalSubscribers.Keys)
            {
                tasks.Add(SafeInvokeAsync(() => handler(eventName, cancellationToken), $"subscribe-all handler for '{eventName}'"));
            }

            if (tasks.Count > 0)
            {
                await Task.WhenAll(tasks);
            }

            metrics?.IncrementEventsProcessed(eventName);
        }
        finally
        {
            stopwatch.Stop();
            metrics?.RecordProcessingTime(stopwatch.Elapsed.TotalMilliseconds, eventName);
        }
    }

    private async Task SafeInvokeAsync(Func<CancellationToken, Task> handler, CancellationToken cancellationToken, string handlerDescription)
    {
        try
        {
            await handler(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in {HandlerDescription}", handlerDescription);
            // Extract event name from description for metrics
            var eventName = ExtractEventNameFromDescription(handlerDescription);
            metrics?.IncrementHandlerErrors(eventName);
        }
    }

    private async Task SafeInvokeAsync(Func<Task> handler, string handlerDescription)
    {
        try
        {
            await handler();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in {HandlerDescription}", handlerDescription);
            var eventName = ExtractEventNameFromDescription(handlerDescription);
            metrics?.IncrementHandlerErrors(eventName);
        }
    }

    private static string ExtractEventNameFromDescription(string description)
    {
        // Extract event name from descriptions like "subscribe for 'EventName'" or "subscribe-all handler for 'EventName'"
        var start = description.IndexOf('\'');
        if (start == -1) return "unknown";
        var end = description.IndexOf('\'', start + 1);
        return end == -1 ? "unknown" : description.Substring(start + 1, end - start - 1);
    }

    private DataChannelWrapper<T> GetOrCreateDataChannelWrapper<T>(string key)
    {
        if (dataChannels.TryGetValue(key, out var existing))
        {
            return (DataChannelWrapper<T>)existing;
        }

        var wrapper = new DataChannelWrapper<T>(logger, options, metrics, shutdownTokenSource.Token);
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

        var count = signalOnlySubscribers.Values.Sum(dict => dict.Count) +
                   allSignalSubscribers.Count +
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

    private static string GetEventKey<T>(string eventName) => $"{eventName}:{typeof(T).FullName}";

    private void ThrowIfDisposed()
    {
        if (isDisposed)
        {
            throw new ObjectDisposedException(nameof(EventHub));
        }
    }

    public void Dispose()
    {
        if (isDisposed) return;

        isDisposed = true;

        signalOnlyChannel.Writer.Complete();
        shutdownTokenSource.Cancel();

        try
        {
            signalProcessingTask.Wait(TimeSpan.FromSeconds(5));
        }
        catch (AggregateException ex) when (ex.InnerException is OperationCanceledException)
        {
            // Expected
        }
        catch (TimeoutException)
        {
            logger.LogWarning("Signal processing task did not complete within timeout");
        }

        foreach (var wrapper in dataChannels.Values)
        {
            wrapper.Dispose();
        }

        dataChannels.Clear();
        signalOnlySubscribers.Clear();
        allSignalSubscribers.Clear();
        shutdownTokenSource.Dispose();
        metrics?.Dispose();
    }

    // Internal interface for type-erased channel wrappers
    private interface IChannelWrapper : IDisposable
    {
        int SubscriberCount { get; }
    }

    private sealed class DataChannelWrapper<T> : IChannelWrapper
    {
        private readonly ILogger logger;
        private readonly EventHubOptions options;
        private readonly EventHubMetrics? metrics;
        private readonly Channel<T> channel;
        private readonly ConcurrentDictionary<Func<T, CancellationToken, Task>, byte> subscribers = new();
        private readonly CancellationTokenSource wrapperTokenSource;
        private readonly Task processingTask;
        private volatile bool isDisposed;

        public int SubscriberCount => subscribers.Count;

        public DataChannelWrapper(ILogger logger, EventHubOptions options, EventHubMetrics? metrics, CancellationToken parentToken)
        {
            this.logger = logger;
            this.options = options;
            this.metrics = metrics;
            wrapperTokenSource = CancellationTokenSource.CreateLinkedTokenSource(parentToken);

            channel = CreateChannel();
            processingTask = ProcessDataAsync(wrapperTokenSource.Token);
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
            subscribers.TryAdd(handler, 0);
            return new Unsubscriber(() => subscribers.TryRemove(handler, out _));
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

            await channel.Writer.WriteAsync(data, timeoutCts?.Token ?? cancellationToken);
        }

        private async Task ProcessDataAsync(CancellationToken cancellationToken)
        {
            try
            {
                await foreach (var data in channel.Reader.ReadAllAsync(cancellationToken))
                {
                    await ProcessSingleDataAsync(data, cancellationToken);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // Expected during shutdown
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in data processing loop for type {Type}", typeof(T).Name);
            }
        }

        private async Task ProcessSingleDataAsync(T data, CancellationToken cancellationToken)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var tasks = new List<Task>();
            var eventName = $"{typeof(T).Name}";

            try
            {
                foreach (var handler in subscribers.Keys)
                {
                    tasks.Add(SafeInvokeHandlerAsync(handler, data, cancellationToken));
                }

                if (tasks.Count > 0)
                {
                    await Task.WhenAll(tasks);
                }

                metrics?.IncrementEventsProcessed(eventName);
            }
            finally
            {
                stopwatch.Stop();
                metrics?.RecordProcessingTime(stopwatch.Elapsed.TotalMilliseconds, eventName);
            }
        }

        private async Task SafeInvokeHandlerAsync(Func<T, CancellationToken, Task> handler, T data, CancellationToken cancellationToken)
        {
            try
            {
                await handler(data, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in data handler for type {Type}", typeof(T).Name);
                metrics?.IncrementHandlerErrors(typeof(T).Name);
            }
        }

        public void Dispose()
        {
            if (isDisposed) return;

            isDisposed = true;
            channel.Writer.Complete();
            wrapperTokenSource.Cancel();

            try
            {
                processingTask.Wait(TimeSpan.FromSeconds(5));
            }
            catch (Exception ex) when (ex is AggregateException or TimeoutException)
            {
                // Log but don't throw during disposal
                logger.LogWarning(ex, "Error during data channel disposal for type {Type}", typeof(T).Name);
            }

            subscribers.Clear();
            wrapperTokenSource.Dispose();
        }
    }

    private sealed class Unsubscriber(Action unsubscribe) : IDisposable
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

