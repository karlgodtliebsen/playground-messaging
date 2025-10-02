using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

using Messaging.EventHub.Library;
using Messaging.EventHub.Library.EventHubs;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Messaging.EventHub.Benchmark;

public class EventHubBenchmarks
{
    private CancellationToken cancellationToken;

    private BenchmarkTests benchmarkTest = null!;
    private EventHubOptions optionsChannel = null!;
    private EventHubOptions optionsCollection = null!;
    [GlobalSetup]
    public void Setup()
    {
        benchmarkTest = new BenchmarkTests();
        cancellationToken = new CancellationTokenSource().Token;

        optionsChannel = new EventHubOptions
        {
            EnableUseChannel = true
        };

        optionsCollection = new EventHubOptions
        {
            EnableUseChannel = false
        };
    }

    [Benchmark]
    public async Task PublishSignalUsingChannel()
    {
        var eventHubChannel = new EventHubChannel(Options.Create(optionsChannel), NSubstitute.Substitute.For<ILogger<EventHubChannel>>());
        await benchmarkTest.EventChannelBenchmark(eventHubChannel, cancellationToken);
    }

    [Benchmark]
    public async Task PublishSignalUsingCollection()
    {
        var eventHubCollection = new EventHubCollection(Options.Create(optionsCollection), NSubstitute.Substitute.For<ILogger<EventHubCollection>>());
        await benchmarkTest.EventChannelBenchmark(eventHubCollection, cancellationToken);
    }
}

public class Program
{
    public static void Main(string[] args) => BenchmarkRunner.Run<EventHubBenchmarks>();
}