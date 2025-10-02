using FluentAssertions;

using Messaging.EventHub.Library;
using Messaging.EventHub.Library.EventHubs;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Messaging.EventHub.Tests;

public class TestOfEventHubCollection(ITestOutputHelper output)
{
    private readonly ILogger<EventHubCollection> logger = NSubstitute.Substitute.For<ILogger<EventHubCollection>>();
    private readonly CancellationToken cancellationToken = TestContext.Current.CancellationToken;


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
            EnableUseChannel = false
        };
        var eventHub = new EventHubCollection(Options.Create(options), logger);


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
            await Task.Delay(1, cancellationToken);
        }


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
            EnableUseChannel = false
        };

        var eventHub = new EventHubCollection(Options.Create(options), logger);
        var subscription1 = eventHub.Subscribe(@event1, async (int i, CancellationToken ct) =>
        {
            output.WriteLine($"Consumed 1: {@event1} value: {i}");
            consumer1++;
        });

        var subscription2 = eventHub.Subscribe(@event1, async (int i, CancellationToken ct) =>
        {
            output.WriteLine($"Consumed 2: {@event1} value: {i}");
            consumer2++;
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

        consumer1.Should().Be(number);
        consumer2.Should().Be(number * 2);
        consumer3.Should().Be(number);
    }


    [Fact]
    public async Task VerifyUseOfEventHubForSendingDataOnly()
    {
        var consumer1 = 0;
        var consumer2 = 0;
        var options = new EventHubOptions()
        {
            EnableUseChannel = false
        };

        var eventHub = new EventHubCollection(Options.Create(options), logger);

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
            EnableUseChannel = false
        };

        var eventHub = new EventHubCollection(Options.Create(options), logger);

        var subscription1 = eventHub.Subscribe<IList<TestEventData>>(async (data, ct) =>
        {
            output.WriteLine($"Consumed 1: {data.GetType().FullName}");
            consumer1++;
        });


        var subscription2 = eventHub.Subscribe<IList<TestEventData>>(async (data, ct) =>
        {
            output.WriteLine($"Consumed 2: {data.GetType().FullName}");
            consumer2++;
        });


        var number = 5;

        for (var i = 0; i < number; i++)
        {
            var data = new TestEventData();
            IList<TestEventData> collection = new List<TestEventData>() { data };
            await eventHub.Publish<IList<TestEventData>>(collection, cancellationToken);
        }


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
            EnableUseChannel = false
        };
        var eventHub = new EventHubCollection(Options.Create(options), logger);

        var subscription1 = eventHub.Subscribe<List<TestEventData>>(async (data, ct) =>
        {
            output.WriteLine($"Consumed 1: {data.GetType().FullName}");
            consumer1++;
        });


        var subscription2 = eventHub.Subscribe<IList<TestEventData>>(async (data, ct) =>
        {
            output.WriteLine($"Consumed 2: {data.GetType().FullName}");
            consumer2++;
        });


        var number = 5;

        for (var i = 0; i < number; i++)
        {
            var data = new TestEventData();
            var collection = new List<TestEventData>() { data };
            await eventHub.Publish<IList<TestEventData>>(collection, cancellationToken);
        }

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
            EnableUseChannel = false
        };
        var eventHub = new EventHubCollection(Options.Create(options), logger);
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
        }

        consumer1.Should().Be(number);
        consumer2.Should().Be(number * 2);
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
            EnableUseChannel = false
        };
        var eventHub = new EventHubCollection(Options.Create(options), logger);
        var disposables = new List<IDisposable>();

        for (int i = 0; i < numberRuns; i++)
        {
            IDisposable subscription = eventHub.Subscribe(@event, async (ct) =>
            {
                output.WriteLine($"Consumed 1: {@event}");
                consumer++;
                await Task.Delay(1, ct);
            });
            disposables.Add(subscription);
        }

        for (var i = 0; i < numberRuns; i++)
        {
            await eventHub.Publish(@event, cancellationToken);
            output.WriteLine($"Publish: {@event}");
        }
        consumer.Should().Be(numberRuns * numberRuns);
        foreach (var disposable in disposables)
        {
            disposable.Dispose();
        }
    }
}