using FluentAssertions;

using Messaging.EventHub.Library;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using System.Threading.Channels;

namespace Messaging.Tests;

public class TestOfEventHub(ITestOutputHelper output)
{
    private readonly ILogger<EventHub.Library.EventHub> logger = NSubstitute.Substitute.For<ILogger<EventHub.Library.EventHub>>();
    private readonly CancellationToken cancellationToken = TestContext.Current.CancellationToken;

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
        var options = new EventHubOptions();
        var eventHub = new EventHub.Library.EventHub(logger, Options.Create(options));


        var subscription1 = eventHub.Subscribe(@event1, async (ct) =>
        {
            output.WriteLine($"Consumed 1: {@event1}");
            consumer1++;
            await Task.Delay(1, ct);
        });
        var subscription2 = eventHub.Subscribe(@event1, async (ct) =>
        {
            output.WriteLine($"Consumed 2: {@event1}");
            consumer2++;
            await Task.Delay(1, ct);
        });
        var subscription3 = eventHub.Subscribe(@event2, async (ct) =>
        {
            output.WriteLine($"Consumed 3: {@event2}");
            consumer3++;
            await Task.Delay(1, ct);
        });


        for (var i = 0; i < numberRuns; i++)
        {
            await eventHub.Publish(@event1, cancellationToken);
            await Task.Delay(1, cancellationToken);
        }

        await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken); //allows the channel to be processed completely

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
            await Task.Delay(1, cancellationToken);
        }
        await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);

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
        var options = new EventHubOptions();
        var eventHub = new EventHub.Library.EventHub(logger, Options.Create(options));
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

        var number = 5;

        for (var i = 0; i < number; i++)
        {
            await eventHub.Publish(@event1, i, cancellationToken);
            await Task.Delay(1, cancellationToken);
        }

        await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken); //allows the channel to be processed completely

        consumer1.Should().Be(number);
        consumer2.Should().Be(number);
        consumer3.Should().Be(0);

        subscription1.Dispose();
        output.WriteLine("disposed Consumer 1");

        for (var i = 0; i < number; i++)
        {
            await eventHub.Publish(@event1, i, cancellationToken);
            await eventHub.Publish(@event2, i, cancellationToken);
            await Task.Delay(1, cancellationToken);
        }

        await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
        consumer1.Should().Be(number);
        consumer2.Should().Be(number * 2);
        consumer3.Should().Be(number);
    }

    [Fact]
    public async Task VerifyUseOfEventHubForSendingEventsToAllSubscribers()
    {
        var consumer1 = 0;
        var consumer2 = 0;
        var @event1 = "TestEvent1";
        var options = new EventHubOptions();
        var eventHub = new EventHub.Library.EventHub(logger, Options.Create(options));
        eventHub.Subscribe(@event1, async (int i, CancellationToken ct) =>
        {
            output.WriteLine($"Consumed 1: {@event1} value: {i}");
            consumer1++;
            await Task.Delay(1, ct);
        });

        eventHub.SubscribeToAll(async (eventName, ct) =>
        {
            output.WriteLine($"Consumed All: {@event1} value: {eventName}");
            consumer2++;
            await Task.Delay(1, ct);
        });

        eventHub.SubscribeToAll((eventName, ct) =>
        {
            logger.LogInformation("EventListener Received Event: {eventName}", eventName);
            return Task.CompletedTask;
        });


        var number = 5;

        for (var i = 0; i < number; i++)
        {
            await eventHub.Publish(@event1, i, cancellationToken);
            await eventHub.Publish(@event1, cancellationToken);
            await Task.Delay(1, cancellationToken);
        }

        await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken); //allows the channel to be processed completely

        consumer1.Should().Be(number);
        consumer2.Should().Be(number * 2);
    }

}