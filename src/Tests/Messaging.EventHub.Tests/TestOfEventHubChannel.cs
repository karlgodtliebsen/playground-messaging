using FluentAssertions;

using Messaging.EventHub.Library;
using Messaging.EventHub.Library.EventHubs;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using System.Threading.Channels;

namespace Messaging.EventHub.Tests;

public class TestOfEventHubChannel(ITestOutputHelper output)
{
    private readonly ILogger<EventHubChannel> logger = NSubstitute.Substitute.For<ILogger<EventHubChannel>>();
    private readonly CancellationToken cancellationToken = TestContext.Current.CancellationToken;

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



    [Fact]
    public async Task VerifyUseOfChannel()
    {
        var channel = Channel.CreateUnbounded<int>();

        var producer = Task.Run(async () =>
        {
            for (var i = 0; i < 10; i++)
            {
                await channel.Writer.WriteAsync(i, cancellationToken);
                output.WriteLine($"Produced: {i}");
                await Task.Delay(100, cancellationToken); // Simulate work
            }

            // Signal that no more items will be written.
            channel.Writer.Complete();
        }, cancellationToken);

        // Consumer Task: reads numbers from the channel.
        var consumer = Task.Run(async () =>
        {
            // ReadAllAsync will keep iterating until the channel is marked complete.
            await foreach (var item in channel.Reader.ReadAllAsync(cancellationToken))
            {
                output.WriteLine($"Consumed: {item}");
                await Task.Delay(150, cancellationToken); // Simulate work
            }
        }, cancellationToken);

        // Wait for both tasks to complete.
        await Task.WhenAll(producer, consumer);
    }

    [Fact]
    public async Task VerifyUseOfEventHubForSendingEventsOnly()
    {
        var consumer1 = 0;
        var consumer2 = 0;
        var consumer3 = 0;
        var number1 = 5;
        var number2 = 5;
        var number3 = 5;
        var numberRuns = 5;

        var @event1 = "TestEvent1";
        var @event2 = "TestEvent2";
        var options = new EventHubOptions()
        {
            EnableUseChannel = true
        };

        var eventHub = new EventHubChannel(Options.Create(options), logger);


        var subscription1 = eventHub.Subscribe(@event1, async (ct) =>
        {
            output.WriteLine($"Consumed 1: {@event1}");
            consumer1++;
        });
        var subscription2 = eventHub.Subscribe(@event1, async (ct) =>
        {
            output.WriteLine($"Consumed 2: {@event1}");
            consumer2++;
        });
        var subscription3 = eventHub.Subscribe(@event2, async (ct) =>
        {
            output.WriteLine($"Consumed 3: {@event2}");
            consumer3++;
        });


        for (var i = 0; i < numberRuns; i++)
        {
            await eventHub.Publish(@event1, cancellationToken);
            //await Task.Delay(1, cancellationToken);
        }
        //await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken); //allows the channel to be processed completely

        await WaitUntilProcessedAsync(eventHub, @event1, numberRuns, TimeSpan.FromSeconds(1), cancellationToken);

        consumer1.Should().Be(number1);
        consumer2.Should().Be(number2);
        consumer3.Should().Be(0);
        //now remove TimeSpan.FromSeconds(1) to verify that it does not receive more events
        subscription1.Dispose();
        output.WriteLine("disposed Consumer 1");

        consumer1 = number1 = 0;
        number2 *= 2;


        for (var i = 0; i < numberRuns; i++)
        {
            await eventHub.Publish(@event1, cancellationToken);
            await eventHub.Publish(@event2, cancellationToken);
        }
        await WaitUntilProcessedAsync(eventHub, @event1, numberRuns, TimeSpan.FromSeconds(1), cancellationToken);
        //        await WaitUntilProcessedAsync(eventHub, @event2, numberRuns, TimeSpan.FromSeconds(1), cancellationToken);
        consumer1.Should().Be(number1);
        consumer2.Should().Be(number2);
        consumer3.Should().Be(number3);
    }

    [Fact]
    public async Task VerifyUseOfEventHubForSendingEventsWithData()
    {
        var consumer1 = 0;
        var consumer2 = 0;
        var consumer3 = 0;
        var @event1 = "TestEvent1";
        var @event2 = "TestEvent2";
        var options = new EventHubOptions()
        {
            EnableUseChannel = true
        };
        var eventHub = new EventHubChannel(Options.Create(options), logger);
        var subscription1 = eventHub.Subscribe(@event1, async (int i, CancellationToken ct) =>
        {
            output.WriteLine($"Consumed 1: {@event1} value: {i}");
            consumer1++;
            await Task.Delay(1, ct);
        });

        var subscription2 = eventHub.Subscribe(@event1, async (int i, CancellationToken ct) =>
        {
            output.WriteLine($"Consumed 2: {@event1} value: {i}");
            consumer2++;
            await Task.Delay(1, ct);
        });
        var subscription3 = eventHub.Subscribe(@event2, async (int i, CancellationToken ct) =>
        {
            output.WriteLine($"Consumed 3: {@event2} value: {i}");
            consumer3++;
            await Task.Delay(1, ct);
        });

        var numberRuns = 5;

        for (var i = 0; i < numberRuns; i++)
        {
            await eventHub.Publish(@event1, i, cancellationToken);
        }

        await WaitUntilProcessedAsync(eventHub, @event1, numberRuns, TimeSpan.FromSeconds(1), cancellationToken);

        consumer1.Should().Be(numberRuns);
        consumer2.Should().Be(numberRuns);
        consumer3.Should().Be(0);

        subscription1.Dispose();
        output.WriteLine("disposed Consumer 1");

        for (var i = 0; i < numberRuns; i++)
        {
            await eventHub.Publish(@event1, i, cancellationToken);
            await eventHub.Publish(@event2, i, cancellationToken);
        }

        await WaitUntilProcessedAsync(eventHub, @event1, numberRuns, TimeSpan.FromSeconds(1), cancellationToken);
        consumer1.Should().Be(numberRuns);
        consumer2.Should().Be(numberRuns * 2);
        consumer3.Should().Be(numberRuns);
    }


    [Fact]
    public async Task VerifyUseOfEventHubForSendingDataOnly()
    {
        var consumer1 = 0;
        var consumer2 = 0;
        var options = new EventHubOptions()
        {
            EnableUseChannel = true
        };
        var eventHub = new EventHubChannel(Options.Create(options), logger);

        var subscription1 = eventHub.Subscribe<TestEventData>(async (data, ct) =>
        {
            output.WriteLine($"Consumed 1: {data.GetType().FullName}");
            consumer1++;
        });


        var subscription2 = eventHub.Subscribe<TestEventData>(async (data, ct) =>
        {
            output.WriteLine($"Consumed 2: {data.GetType().FullName}");
            consumer2++;
        });


        var number = 5;

        for (var i = 0; i < number; i++)
        {
            var data = new TestEventData();
            await eventHub.Publish<TestEventData>(data, cancellationToken);
        }

        //await WaitUntilProcessedAsync(eventHub, @event1, numberRuns, TimeSpan.FromSeconds(1), cancellationToken);
        await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken); //allows the channel to be processed completely

        consumer1.Should().Be(number);
        consumer2.Should().Be(number);
    }
    [Fact]
    public async Task VerifyUseOfEventHubForSendingAbstractCollectionOfDataOnly()
    {
        var consumer1 = 0;
        var consumer2 = 0;
        var options = new EventHubOptions()
        {
            EnableUseChannel = true
        };
        var eventHub = new EventHubChannel(Options.Create(options), logger);

        var subscription1 = eventHub.Subscribe<IList<TestEventData>>((data, ct) =>
        {
            output.WriteLine($"Consumed 1: {data.GetType().FullName}");
            consumer1++;
            return Task.CompletedTask;
        });


        var subscription2 = eventHub.Subscribe<IList<TestEventData>>((data, ct) =>
        {
            output.WriteLine($"Consumed 2: {data.GetType().FullName}");
            consumer2++;
            return Task.CompletedTask;
        });


        var number = 5;

        for (var i = 0; i < number; i++)
        {
            var data = new TestEventData();
            IList<TestEventData> collection = new List<TestEventData>() { data };
            await eventHub.Publish<IList<TestEventData>>(collection, cancellationToken);
        }

        await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken); //allows the channel to be processed completely

        consumer1.Should().Be(number);
        consumer2.Should().Be(number);
    }

    [Fact]
    public async Task VerifyUseOfEventHubForSendingCollectionOfDataOnly()
    {
        var consumer1 = 0;
        var consumer2 = 0;
        var options = new EventHubOptions()
        {
            EnableUseChannel = true
        };
        var eventHub = new EventHubChannel(Options.Create(options), logger);

        var subscription1 = eventHub.Subscribe<List<TestEventData>>((data, ct) =>
        {
            output.WriteLine($"Consumed 1: {data.GetType().FullName}");
            consumer1++;
            return Task.CompletedTask;
        });


        var subscription2 = eventHub.Subscribe<IList<TestEventData>>((data, ct) =>
        {
            output.WriteLine($"Consumed 2: {data.GetType().FullName}");
            consumer2++;
            return Task.CompletedTask;
        });


        var number = 5;

        for (var i = 0; i < number; i++)
        {
            var data = new TestEventData();
            var collection = new List<TestEventData>() { data };
            await eventHub.Publish<IList<TestEventData>>(collection, cancellationToken);
        }

        await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken); //allows the channel to be processed completely

        consumer1.Should().Be(0);
        consumer2.Should().Be(number);
    }

    [Fact]
    public async Task VerifyUseOfEventHubForSendingEventsToAllSubscribers()
    {
        var consumer1 = 0;
        var consumer2 = 0;
        var @event1 = "TestEvent1";
        var options = new EventHubOptions()
        {
            EnableUseChannel = true
        };
        var eventHub = new EventHubChannel(Options.Create(options), logger);
        eventHub.Subscribe(@event1, (int i, CancellationToken ct) =>
        {
            output.WriteLine($"Consumed 1: {@event1} value: {i}");
            consumer1++;
            return Task.CompletedTask;
        });

        eventHub.SubscribeToAll((eventName, ct) =>
        {
            output.WriteLine($"Consumed All: {@event1} value: {eventName}");
            consumer2++;
            return Task.CompletedTask;
        });

        eventHub.SubscribeToAll((eventName, ct) =>
        {
            logger.LogInformation("EventListener Received Event: {eventName}", eventName);
            return Task.CompletedTask;
        });


        var numberRuns = 5;

        for (var i = 0; i < numberRuns; i++)
        {
            await eventHub.Publish(@event1, i, cancellationToken);
            await eventHub.Publish(@event1, cancellationToken);
        }

        await WaitUntilProcessedAsync(eventHub, @event1, numberRuns, TimeSpan.FromSeconds(1), cancellationToken);
        // await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken); //allows the channel to be processed completely

        consumer1.Should().Be(numberRuns);
        consumer2.Should().Be(numberRuns * 2);
    }
    [Fact]
    public async Task VerifyUseOfEventHubWithMultipleSubscribersForSendingEventsToAllSubscribers()
    {
        // Add the code to benchmark here

        var consumer = 0;
        var number = 5;
        var numberRuns = 5;
        var @event = "TestEvent1";
        var options = new EventHubOptions()
        {
            EnableUseChannel = true
        };
        var eventHub = new EventHubChannel(Options.Create(options), logger);
        var disposables = new List<IDisposable>();

        for (int i = 0; i < numberRuns; i++)
        {
            IDisposable subscription = eventHub.Subscribe(@event, async (ct) =>
            {
                output.WriteLine($"Consumed 1: {@event}");
                consumer++;
            });
            disposables.Add(subscription);
        }

        for (var i = 0; i < numberRuns; i++)
        {
            await eventHub.Publish(@event, cancellationToken);
            output.WriteLine($"Publish: {@event}");
        }
        await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
        consumer.Should().Be(numberRuns * numberRuns);
        foreach (var disposable in disposables)
        {
            disposable.Dispose();
        }
    }
}