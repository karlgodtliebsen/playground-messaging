using Microsoft.CSharp.RuntimeBinder;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using System.Collections.Concurrent;
using System.Diagnostics;

namespace Messaging.EventHub.Library.EventHubs;

public sealed class EventHubCollection : IEventHub
{
    private readonly ConcurrentDictionary<string, IList<Func<CancellationToken, Task>>> eventSubscribers = new();
    private readonly ConcurrentDictionary<string, IList<object>> dataChannels = new();
    private readonly List<Func<string, CancellationToken, Task>> allEventsSubscribers = [];
    private readonly Lock lockObj = new();
    private EventHubOptions options;
    private ILogger<EventHubCollection> logger;
    private volatile bool isDisposed;
    private readonly EventHubMetrics? metrics;
    private readonly CancellationTokenSource shutdownTokenSource = new();

    public EventHubCollection(IOptions<EventHubOptions> options, ILogger<EventHubCollection> logger)
    {
        this.logger = logger;
        this.options = options.Value;
    }

    public EventHubCollection(EventHubMetrics metrics, IOptions<EventHubOptions> options, ILogger<EventHubCollection> logger) : this(options, logger)
    {
        this.metrics = metrics;
    }

    internal void Unsubscribe(string eventName, Func<CancellationToken, Task> handler)
    {
        if (eventSubscribers.TryGetValue(eventName, out var list))
        {
            using (lockObj.EnterScope())
            {
                list.Remove(handler);
                if (list.Count == 0)
                {
                    eventSubscribers.TryRemove(eventName, out _);
                }
            }
        }
    }

    internal void Unsubscribe<T>(string eventName, Func<T, CancellationToken, Task> handler)
    {
        var key = KeyGenerator.GetEventKey<T>(eventName);
        if (dataChannels.TryGetValue(key, out var list))
        {
            using (lockObj.EnterScope())
            {
                list.Remove(handler);
                if (list.Count == 0)
                {
                    dataChannels.TryRemove(key, out _);
                }
            }
        }
    }

    internal void UnsubscribeFromAll(Func<string, CancellationToken, Task> handler)
    {
        using (lockObj.EnterScope())
        {
            if (allEventsSubscribers.Contains(handler))
            {
                allEventsSubscribers.Remove(handler);
            }
        }
    }

    public IDisposable Subscribe(string eventName, Func<CancellationToken, Task> handler)
    {
        ThrowIfDisposed();
        ArgumentException.ThrowIfNullOrWhiteSpace(eventName);
        ArgumentNullException.ThrowIfNull(handler);

        eventSubscribers.AddOrUpdate(
            eventName,
            new List<Func<CancellationToken, Task>> { handler },
            (key, list) =>
            {
                using (lockObj.EnterScope())
                {
                    list.Add(handler);
                    return list;
                }
            });
        UpdateSubscriberMetrics();
        UpdateChannelMetrics();
        return new UnSubscriber(() =>
        {
            Unsubscribe(eventName, handler);
            UpdateSubscriberMetrics();
        });

    }

    public IDisposable Subscribe<T>(string eventName, Func<T, CancellationToken, Task> handler)
    {
        var key = KeyGenerator.GetEventKey<T>(eventName);
        dataChannels.AddOrUpdate(
            key,
            new List<object> { handler },
            (_, list) =>
            {
                using (lockObj.EnterScope())
                {
                    list.Add(handler);
                    return list;
                }
            });
        return new UnSubscriber(() =>
        {
            Unsubscribe(eventName, handler);
            UpdateSubscriberMetrics();
            UpdateChannelMetrics();
        });
    }

    public IDisposable Subscribe<T>(Func<T, CancellationToken, Task> handler)
    {
        var eventName = KeyGenerator.GetEventKey<T>();
        dataChannels.AddOrUpdate(
            eventName,
            new List<object> { handler },
            (_, list) =>
            {
                using (lockObj.EnterScope())
                {
                    list.Add(handler);
                    return list;
                }
            });
        return new UnSubscriber(() =>
        {
            Unsubscribe(eventName, handler);
            UpdateSubscriberMetrics();
            UpdateChannelMetrics();
        });
    }

    public IDisposable SubscribeToAll(Func<string, CancellationToken, Task> handler)
    {
        using (lockObj.EnterScope())
        {
            allEventsSubscribers.Add(handler);
        }

        return new UnSubscriber(() =>
        {
            UnsubscribeFromAll(handler);
            UpdateSubscriberMetrics();
            UpdateChannelMetrics();
        });
    }

    private async Task BroadCastEvent(string eventName, CancellationToken cancellationToken = default)
    {
        List<Func<string, CancellationToken, Task>> actionsSnapshot;
        using (lockObj.EnterScope())
        {
            actionsSnapshot = [.. allEventsSubscribers];
        }

        foreach (var action in actionsSnapshot)
        {
            await action.Invoke(eventName, cancellationToken);
        }
    }


    public async Task Publish(string eventName, CancellationToken cancellationToken = default)
    {
        // Increment published metric
        metrics?.IncrementEventsPublished(eventName);

        if (eventSubscribers.TryGetValue(eventName, out var handlers))
        {
            List<Func<CancellationToken, Task>> actionsSnapshot;
            using (lockObj.EnterScope())
            {
                actionsSnapshot = [.. handlers];
            }

            foreach (var action in actionsSnapshot)
            {
                await action.Invoke(cancellationToken);
            }
        }

        await BroadCastEvent(eventName, cancellationToken);
    }

    public Task Publish(object data, CancellationToken cancellationToken = default)
        => Publish(data.GetType().FullName!, data, cancellationToken);

    private void ThrowIfDisposed()
    {
        if (isDisposed)
        {
            throw new ObjectDisposedException(nameof(EventHubCollection));
        }
    }

    public void Dispose() => DisposeAsync().AsTask().GetAwaiter().GetResult();

    public ValueTask DisposeAsync()
    {
        if (isDisposed) return ValueTask.CompletedTask;

        isDisposed = true;

        eventSubscribers.Clear();
        this.dataChannels.Clear();
        this.allEventsSubscribers.Clear();
        shutdownTokenSource.Dispose();
        metrics?.Dispose();
        return ValueTask.CompletedTask;
    }

    private void UpdateSubscriberMetrics()
    {
        if (metrics == null) return;
        var signalSubs = eventSubscribers.Values.Sum(dict => dict.Count) + eventSubscribers.Count;
        var dataSubs = dataChannels.Values.Sum(inner => inner.Count);
        metrics.SetSubscriberCount(signalSubs + dataSubs);
    }

    private void UpdateChannelMetrics()
    {
        if (metrics == null) return;

        var channelCount = dataChannels.Values.Sum(inner => inner.Count);
        metrics.SetChannelCount(channelCount);
    }

    public async Task Publish<T>(T data, CancellationToken cancellationToken = default)
    {
        var key = KeyGenerator.GetEventKey<T>();
        await Publish<T>(key, key, data, cancellationToken);
    }

    public async Task Publish<T>(string eventName, T data, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(data);
        var key = KeyGenerator.GetEventKey<T>(eventName);
        await Publish<T>(key, eventName, data, cancellationToken);
    }


    public async Task Publish<T>(string key, string eventName, T data, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(data);

        // START METRICS - Increment published count
        metrics?.IncrementEventsPublished(eventName);

        var sw = Stopwatch.StartNew();
        var handlerCount = 0;

        try
        {
            if (dataChannels.TryGetValue(key, out var actions))
            {
                List<object> actionsSnapshot;
                using (lockObj.EnterScope())
                {
                    actionsSnapshot = [.. actions];
                }

                foreach (var action in actionsSnapshot)
                {
                    handlerCount++;
                    var handlerSw = Stopwatch.StartNew();

                    try
                    {
                        if (action is Func<T, CancellationToken, Task> typedAction)
                        {
                            await typedAction.Invoke(data, cancellationToken);
                        }
                        else
                        {
                            await ((dynamic)action).Invoke((dynamic)data, cancellationToken);
                        }

                        // Record successful processing
                        handlerSw.Stop();
                        metrics?.RecordProcessingTime(handlerSw.Elapsed.TotalMilliseconds, eventName);
                        metrics?.IncrementEventsProcessed(eventName);
                    }
                    catch (RuntimeBinderException ex)
                    {
                        logger.LogError(ex, "Action invocation failed due to type mismatch for event {EventName}", eventName);
                        metrics?.IncrementHandlerErrors(eventName);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Action invocation failed for event {EventName}", eventName);
                        metrics?.IncrementHandlerErrors(eventName);
                    }
                }
            }

            await BroadCastEvent(eventName, cancellationToken);
        }
        finally
        {
            sw.Stop();

            // Log summary if there were handlers
            if (handlerCount > 0)
            {
                logger.LogDebug("Published {EventName}: {HandlerCount} handlers in {Duration}ms",
                    eventName, handlerCount, sw.Elapsed.TotalMilliseconds);
            }
        }
    }


    internal static class KeyGenerator
    {
        private const string Separator = ":";
        public static string GetEventKey<T>(string eventName) => $"{eventName}{Separator}{typeof(T).FullName}";

        public static string GetEventKey<T>() => $"data{Separator}{typeof(T).FullName}";
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