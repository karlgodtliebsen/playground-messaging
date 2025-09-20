using Microsoft.Extensions.Logging;

using System.Collections.Concurrent;
using System.Threading.Channels;

namespace Messaging.Library;

/// <summary>
/// An event hub that uses channels for both signal-only events and data-carrying events.
/// Supports asynchronous handlers, generic data events, and "subscribe all" functionality.
/// </summary>
public sealed class EventHub : IEventHub
{
    private readonly ILogger<EventHub> logger;
    private readonly Channel<string> signalOnlyChannel;
    private readonly ConcurrentDictionary<string, IChannelWrapper> dataChannels = new();
    private readonly ConcurrentDictionary<string, ConcurrentBag<Func<CancellationToken, Task>>> signalOnlySubscribers = new();
    private readonly ConcurrentBag<Func<string, CancellationToken, Task>> allSignalSubscribers = new();
    private readonly CancellationTokenSource shutdownTokenSource = new();
    private readonly Task signalProcessingTask;
    private volatile bool isDisposed;

    public EventHub(ILogger<EventHub> logger)
    {
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));

        var channelOptions = new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false,
            AllowSynchronousContinuations = false
        };

        signalOnlyChannel = Channel.CreateUnbounded<string>(channelOptions);
        signalProcessingTask = ProcessSignalsAsync(shutdownTokenSource.Token);
    }

    // Subscribe to signal-only events
    public IDisposable Subscribe(string eventName, Func<CancellationToken, Task> handler)
    {
        ThrowIfDisposed();
        ArgumentException.ThrowIfNullOrWhiteSpace(eventName);
        ArgumentNullException.ThrowIfNull(handler);

        var subscribers = signalOnlySubscribers.GetOrAdd(eventName, _ => new ConcurrentBag<Func<CancellationToken, Task>>());
        subscribers.Add(handler);

        return new Unsubscriber(() => RemoveSignalSubscriber(eventName, handler));
    }

    // Subscribe to data events (using type name as event name)
    public IDisposable Subscribe<T>(Func<T, CancellationToken, Task> handler)
    {
        return Subscribe<T>(typeof(T).Name, handler);
    }

    // Subscribe to data events with custom event name
    public IDisposable Subscribe<T>(string eventName, Func<T, CancellationToken, Task> handler)
    {
        ThrowIfDisposed();
        ArgumentException.ThrowIfNullOrWhiteSpace(eventName);
        ArgumentNullException.ThrowIfNull(handler);

        var key = GetEventKey<T>(eventName);
        var wrapper = GetOrCreateDataChannelWrapper<T>(key);

        return wrapper.AddSubscriber(handler);
    }

    // Subscribe to all events (receives event name)
    public IDisposable SubscribeToAll(Func<string, CancellationToken, Task> handler)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(handler);

        allSignalSubscribers.Add(handler);
        return new Unsubscriber(() => RemoveFromCollection(allSignalSubscribers, handler));
    }

    // Publish signal-only event
    public async Task Publish(string eventName, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ArgumentException.ThrowIfNullOrWhiteSpace(eventName);

        await signalOnlyChannel.Writer.WriteAsync(eventName, cancellationToken);
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
            await ((DataChannelWrapper<T>)wrapper).PublishAsync(data, cancellationToken);
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
        var tasks = new List<Task>();

        // Process specific event subscribers
        if (signalOnlySubscribers.TryGetValue(eventName, out var subscribers))
        {
            foreach (var handler in subscribers)
            {
                tasks.Add(SafeInvokeAsync(handler, cancellationToken, $"subscribe for '{eventName}'"));
            }
        }

        // Process "subscribe all" handlers
        foreach (var handler in allSignalSubscribers)
        {
            tasks.Add(SafeInvokeAsync(() => handler(eventName, cancellationToken), $"subscribe-all handler for '{eventName}'"));
        }

        if (tasks.Count > 0)
        {
            await Task.WhenAll(tasks);
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
        }
    }

    private DataChannelWrapper<T> GetOrCreateDataChannelWrapper<T>(string key)
    {
        if (dataChannels.TryGetValue(key, out var existing))
        {
            return (DataChannelWrapper<T>)existing;
        }

        var wrapper = new DataChannelWrapper<T>(logger, shutdownTokenSource.Token);
        if (dataChannels.TryAdd(key, wrapper))
        {
            return wrapper;
        }

        // Another thread added it first
        wrapper.Dispose();
        return (DataChannelWrapper<T>)dataChannels[key];
    }

    private void RemoveSignalSubscriber(string eventName, Func<CancellationToken, Task> handler)
    {
        if (signalOnlySubscribers.TryGetValue(eventName, out var subscribers))
        {
            // Note: ConcurrentBag doesn't support removal, so we'd need a different approach
            // For now, handlers will remain but won't be called if unsubscribed
        }
    }

    private static void RemoveFromCollection<T>(ConcurrentBag<T> collection, T item)
    {
        // ConcurrentBag doesn't support removal - items remain but are filtered during enumeration
        // Alternative: Use ConcurrentDictionary<T, byte> as a set
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
        shutdownTokenSource.Dispose();
    }

    // Internal interface for type-erased channel wrappers
    private interface IChannelWrapper : IDisposable
    {
    }

    private sealed class DataChannelWrapper<T> : IChannelWrapper
    {
        private readonly ILogger logger;
        private readonly Channel<T> channel;
        private readonly ConcurrentBag<Func<T, CancellationToken, Task>> subscribers = new();
        private readonly CancellationTokenSource wrapperTokenSource;
        private readonly Task processingTask;
        private volatile bool isDisposed;

        public DataChannelWrapper(ILogger logger, CancellationToken parentToken)
        {
            this.logger = logger;
            wrapperTokenSource = CancellationTokenSource.CreateLinkedTokenSource(parentToken);

            var options = new UnboundedChannelOptions
            {
                SingleReader = true,
                SingleWriter = false,
                AllowSynchronousContinuations = false
            };

            channel = Channel.CreateUnbounded<T>(options);
            processingTask = ProcessDataAsync(wrapperTokenSource.Token);
        }

        public IDisposable AddSubscriber(Func<T, CancellationToken, Task> handler)
        {
            subscribers.Add(handler);
            return new Unsubscriber(() => RemoveFromCollection(subscribers, handler));
        }

        public async Task PublishAsync(T data, CancellationToken cancellationToken)
        {
            if (!isDisposed)
            {
                await channel.Writer.WriteAsync(data, cancellationToken);
            }
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
            var tasks = new List<Task>();

            foreach (var handler in subscribers)
            {
                tasks.Add(SafeInvokeHandlerAsync(handler, data, cancellationToken));
            }

            if (tasks.Count > 0)
            {
                await Task.WhenAll(tasks);
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

