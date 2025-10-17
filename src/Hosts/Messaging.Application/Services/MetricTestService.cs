using Microsoft.Extensions.Logging;

using System.Diagnostics.Metrics;

namespace Messaging.Application.Services;

public class MetricTestService(IMeterFactory meterFactory, ILogger<MetricTestService> logger)
{
    private Meter meter;
    private Counter<long> testCounter;
    private readonly ILogger logger = logger;

    public void Initialize(string name)
    {
        meter = meterFactory.Create(name, "1.0.0");
        testCounter = meter.CreateCounter<long>("test_counter", "count", "Test counter for debugging");
        this.logger.LogInformation("MetricTestService initialized with meter: {MeterName}", meter.Name);
    }

    public void IncrementTest()
    {
        testCounter.Add(1);
        logger.LogTrace("Test counter incremented");
    }
}
