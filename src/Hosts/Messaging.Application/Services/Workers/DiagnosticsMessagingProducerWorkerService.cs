using Messaging.Application.Services.Hosts;
using Messaging.Domain.Library.DemoMessages;
using Microsoft.Extensions.Logging;

using System.Diagnostics;
using Messaging.Observability.Library.ActivitySourceLogging;
using Wolverine;

namespace Messaging.Application.Services.Workers;

public sealed class DiagnosticsMessagingProducerWorkerService(IActivitySourceFactory factory, MetricTestService metricsService, IMessageBus messageBus, ILogger<OrderDomainProducerWorkerService> logger)
{
    public async Task Run(CancellationToken cancellationToken)
    {
        var serviceName = nameof(DiagnosticsMessagingProducerServiceHost);
        logger.LogInformation("Worker Service:{service} is starting.", serviceName);
        metricsService.Initialize("diagnostics-messages-producer");
        while (!cancellationToken.IsCancellationRequested)
        {
            {
                using var activity = factory.CreateActivity(serviceName, ActivityKind.Producer, "Creating Diagnostics Messages");
                await messageBus.PublishAsync(
                    new TextMessage("the host", "the app", "Hello world", DateTimeOffset.UtcNow),
                    new DeliveryOptions
                    {

                    });

                await messageBus.PublishAsync(
                    new PingMessage("the host", "the app", DateTimeOffset.UtcNow),
                    new DeliveryOptions
                    {

                    });


                await messageBus.PublishAsync(
                    new HeartbeatMessage("the host", "the app", DateTimeOffset.UtcNow),
                    new DeliveryOptions
                    {

                    });

            }
            metricsService.IncrementTest();
            await Task.Delay(10, cancellationToken);
        }
    }
}