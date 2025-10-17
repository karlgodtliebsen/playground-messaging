using Messaging.EventHub.Library;
using Microsoft.Extensions.Logging;

using System.Diagnostics;
using Messaging.Observability.Library.ActivitySourceLogging;

namespace Messaging.Application.Services.Workers;

public sealed class MessagingConsumerWorkerService(IActivitySourceFactory factory, MetricTestService metricsService, IEventHub eventHub, ILogger<MessagingConsumerWorkerService> logger)
{
    public async Task Run(CancellationToken cancellationToken)
    {
        var serviceName = nameof(MessagingConsumerWorkerService) + "-" + Ulid.NewUlid(DateTimeOffset.UtcNow).ToString();
        logger.LogInformation("Consumer Worker Service: {service} is starting.", serviceName);
        metricsService.Initialize("messages-consumer");
        while (!cancellationToken.IsCancellationRequested)
        {
            {
                using var activity = factory.CreateActivity(serviceName, ActivityKind.Consumer, "Sending Keep Alive");
                await eventHub.Publish("Alive", serviceName, cancellationToken);
            }
            metricsService.IncrementTest();
            await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);
        }

    }
}