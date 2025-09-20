using Microsoft.Extensions.Logging;

using System.Collections.Concurrent;
using System.Threading.Channels;

namespace Messaging.Library;

/// <summary>
/// An event hub/Signal that uses a central channel (of strings) for signal (events without data),
/// and uses a dedicated channel per event name / generic data type for signal/events with data.
/// Supports asynchronous handlers, generic data events, and "subscribe all" functionality.
/// </summary>
public sealed class SignalChannel : IDisposable, ISignalChannel
{
    private readonly ILogger logger;
    private readonly Channel<string> signalOnlyChannel;
    private readonly ConcurrentDictionary<string, object> channels = new();
    private readonly ConcurrentDictionary<string, List<Func<CancellationToken, Task>>> signalOnlySubscribers = new();
    private readonly List<Action<string>> allSignalSubscribers = new();
    private readonly Lock allSubscribersLock = new();
    private readonly CancellationTokenSource cts = new();
    private readonly Task signalProcessingTask;
    private bool disposed;

    public SignalChannel(ILogger<SignalChannel> logger)
    {
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));

        var options = new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false,
            AllowSynchronousContinuations = false
        };

        signalOnlyChannel = Channel.CreateUnbounded<string>(options);
        signalProcessingTask = Task.Run(() => ProcessSignal(cts.Token));
    }

    private async Task ProcessSignal(CancellationToken ct)
    {
        try
        {
            while (await signalOnlyChannel.Reader.WaitToReadAsync(cts.Token))
            {
                while (signalOnlyChannel.Reader.TryRead(out var eventName))
                {
                    await ProcessSignal(eventName, ct);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in event processing loop");
        }
    }

    private async Task ProcessSignal(string eventName, CancellationToken ct)
    {
        try
        {
            // Process asynchronous subscribers
            if (signalOnlySubscribers.TryGetValue(eventName, out var asyncList))
            {
                List<Func<CancellationToken, Task>> asyncSnapshot;
                lock (asyncList)
                {
                    asyncSnapshot = asyncList.ToList();
                }

                foreach (var func in asyncSnapshot)
                {
                    try
                    {
                        await func(ct);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Error in event-only async subscriber for event {EventName}", eventName);
                    }
                }
            }

            // Process "subscribe all" handlers
            List<Action<string>> allSnapshot;
            lock (allSubscribersLock)
            {
                allSnapshot = allSignalSubscribers.ToList();
            }

            foreach (var action in allSnapshot)
            {
                try
                {
                    action(eventName);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error in subscribe-all handler for event {EventName}", eventName);
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing Signal {EventName}", eventName);
        }
    }

    public IDisposable Subscribe(string signalName, Func<CancellationToken, Task> handler)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(signalName);
        ArgumentNullException.ThrowIfNull(handler);

        signalOnlySubscribers.AddOrUpdate(
            signalName,
            _ => new List<Func<CancellationToken, Task>> { handler },
            (_, list) =>
            {
                lock (list)
                {
                    list.Add(handler);
                }

                return list;
            });

        return new UnSubscriber(() =>
        {
            if (signalOnlySubscribers.TryGetValue(signalName, out var list))
            {
                lock (list)
                {
                    list.Remove(handler);
                    if (list.Count == 0)
                    {
                        signalOnlySubscribers.TryRemove(signalName, out _);
                    }
                }
            }
        });
    }


    public IDisposable Subscribe<T>(Func<T, CancellationToken, Task> handler)
    {
        var signalName = nameof(T);
        return Subscribe<T>(signalName, handler);
    }

    public IDisposable Subscribe<T>(string signalName, Func<T, CancellationToken, Task> handler)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(signalName);
        ArgumentNullException.ThrowIfNull(handler);

        var key = KeyGenerator.GetGenericEventKey<T>(signalName);
        GenericChannelWrapper<T> wrapper;

        if (channels.TryGetValue(key, out var existing))
        {
            wrapper = (GenericChannelWrapper<T>)existing;
        }
        else
        {
            wrapper = new GenericChannelWrapper<T>(logger);
            Task.Run(() => wrapper.ProcessLoop(cts.Token));
            channels.TryAdd(key, wrapper);
        }

        lock (wrapper.Subscribers)
        {
            wrapper.Subscribers.Add(handler);
        }

        return new UnSubscriber(() =>
        {
            lock (wrapper.Subscribers)
            {
                wrapper.Subscribers.Remove(handler);
                if (wrapper.Subscribers.Count == 0)
                {
                    if (channels.TryRemove(key, out var removedWrapper) && removedWrapper is IDisposable disposable)
                    {
                        disposable.Dispose();
                    }
                }
            }
        });
    }

    public IDisposable SubscribeAll(Action<string> handler)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(handler);

        lock (allSubscribersLock)
        {
            allSignalSubscribers.Add(handler);
        }

        return new UnSubscriber(() =>
        {
            lock (allSubscribersLock)
            {
                allSignalSubscribers.Remove(handler);
            }
        });
    }


    public Task Publish(string signal, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(signal);
        return signalOnlyChannel.Writer.WriteAsync(signal, cancellationToken).AsTask();
    }

    public async Task Publish<T>(T data, CancellationToken cancellationToken = default)
    {
        var signal = nameof(T);
        await Publish(signal, data, cancellationToken);
    }

    public async Task Publish<T>(string signal, T data, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(signal);
        ArgumentNullException.ThrowIfNull(data);

        var key = KeyGenerator.GetGenericEventKey<T>(signal);
        if (channels.TryGetValue(key, out var existing))
        {
            var wrapper = (GenericChannelWrapper<T>)existing;
            await wrapper.Channel.Writer.WriteAsync(data, cancellationToken);
        }
        else
        {
            logger.LogWarning("No subscribers for signal {signal} with data type {Type}", signal, typeof(T).Name);
        }
    }


    private void ThrowIfDisposed()
    {
        if (disposed)
        {
            throw new ObjectDisposedException(nameof(SignalChannel));
        }
    }

    public void Dispose()
    {
        if (!disposed)
        {
            disposed = true;
            signalOnlyChannel.Writer.Complete();
            cts.Cancel();

            try
            {
                signalProcessingTask.Wait();
            }
            catch (AggregateException ex) when (ex.InnerException is OperationCanceledException)
            {
                // Ignore cancellation
            }

            foreach (var channel in channels.Values)
            {
                if (channel is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }

            channels.Clear();
            signalOnlySubscribers.Clear();
            allSignalSubscribers.Clear();
            cts.Dispose();
        }
    }


    private sealed class UnSubscriber(Action unsubscribe) : IDisposable
    {
        private readonly Action unsubscribe = unsubscribe ?? throw new ArgumentNullException(nameof(unsubscribe));
        private bool disposed;

        public void Dispose()
        {
            if (!disposed)
            {
                unsubscribe();
                disposed = true;
            }
        }
    }

    private sealed class GenericChannelWrapper<T> : IDisposable
    {
        private readonly ILogger logger;
        private readonly CancellationTokenSource wrapperCts;
        private bool disposed;

        public Channel<T> Channel { get; private set; }
        public List<Func<T, CancellationToken, Task>> Subscribers { get; } = new();

        public GenericChannelWrapper(ILogger logger)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            wrapperCts = new CancellationTokenSource();

            //var options = new UnboundedChannelOptions
            //{
            //    SingleReader = true,
            //    SingleWriter = false,
            //    AllowSynchronousContinuations = false
            //};
            //Channel = Channel.CreateUnbounded<T>(options);

            var options = new UnboundedChannelOptions { SingleReader = false, SingleWriter = false };
            Channel = System.Threading.Channels.Channel.CreateUnbounded<T>(options);

            //var options = new UnboundedChannelOptions { SingleReader = false, SingleWriter = false };
            //Channel = System.Threading.Channels.Channel.CreateUnbounded<T>(options);
        }

        public async Task ProcessLoop(CancellationToken cancellationToken)
        {
            try
            {
                using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, wrapperCts.Token);

                while (await Channel.Reader.WaitToReadAsync(linkedCts.Token))
                {
                    while (Channel.Reader.TryRead(out var data))
                    {
                        await ProcessData(data);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Normal shutdown
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in generic signal processing loop for type {Type}", typeof(T).Name);
            }
        }

        private async Task ProcessData(T data)
        {
            try
            {
                // Process async subscribers
                List<Func<T, CancellationToken, Task>> snapshot;
                lock (Subscribers)
                {
                    snapshot = Subscribers.ToList();
                }

                foreach (var handler in snapshot)
                {
                    try
                    {
                        await handler(data, wrapperCts.Token);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Error in handler for type {Type}", typeof(T).Name);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing data of type {Type}", typeof(T).Name);
            }
        }

        public void Dispose()
        {
            if (!disposed)
            {
                disposed = true;
                Channel.Writer.Complete();
                wrapperCts.Cancel();
                wrapperCts.Dispose();
                Subscribers.Clear();
            }
        }
    }
}

public static class KeyGenerator
{
    public static string GetGenericEventKey<T>(string name) => $"{name}:{typeof(T).FullName}";
}
