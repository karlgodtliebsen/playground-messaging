using BenchmarkDotNet.Attributes;

using Messaging.EventHub.Library;

namespace Messaging.EventHub.Benchmark;

[MemoryDiagnoser]
public class BenchmarkTests
{
    public static async Task WaitUntilProcessedAsync(
        IEventHub hub,
        string eventName,
        int expectedMessages,
        TimeSpan timeout,
        CancellationToken ct = default)
    {
        var remaining = expectedMessages;
        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        IDisposable? sub = null;
        sub = hub.SubscribeToAll(async (name, token) =>
        {
            if (name == eventName)
            {
                if (Interlocked.Decrement(ref remaining) == 0)
                {
                    tcs.TrySetResult();
                }
            }
            await Task.CompletedTask;
        });

        using (sub)
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(timeout);
            await tcs.Task.WaitAsync(cts.Token);
        }
    }

    [Benchmark]
    public async Task EventChannelBenchmark(IEventHub eventHubChannel, CancellationToken cancellationToken)
    {
        var consumer = 0;
        var numberRuns = 50;

        var @event = "TestEvent1";
        var disposables = new List<IDisposable>();

        for (int i = 0; i < numberRuns; i++)
        {
            IDisposable subscription = eventHubChannel.Subscribe(@event, (ct) =>
                {
                    consumer++;
                    return Task.CompletedTask;
                });
            disposables.Add(subscription);
        }

        for (var i = 0; i < numberRuns; i++)
        {
            await eventHubChannel.Publish(@event, cancellationToken);
        }

        // await WaitUntilProcessedAsync(eventHubChannel, @event, numberRuns, TimeSpan.FromSeconds(1), cancellationToken);

        foreach (var disposable in disposables)
        {
            disposable.Dispose();
        }
    }


    [Benchmark]
    public async Task EventCollectionBenchmark(IEventHub eventHubCollection, CancellationToken cancellationToken)
    {
        var consumer = 0;
        var numberRuns = 50;
        var @event = "TestEvent1";
        var disposables = new List<IDisposable>();

        for (int i = 0; i < numberRuns; i++)
        {
            IDisposable subscription = eventHubCollection.Subscribe(@event, (ct) =>
            {
                consumer++;
                return Task.CompletedTask;
            });
            disposables.Add(subscription);
        }

        for (var i = 0; i < numberRuns; i++)
        {
            await eventHubCollection.Publish(@event, cancellationToken);
        }

        foreach (var disposable in disposables)
        {
            disposable.Dispose();
        }
    }
}