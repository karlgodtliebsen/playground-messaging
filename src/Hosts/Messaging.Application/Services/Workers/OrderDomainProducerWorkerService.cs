using Messaging.Application.Services.Hosts;
using Messaging.Domain.Library.Orders;
using Messaging.Domain.Library.Payments;
using Microsoft.Extensions.Logging;

using System.Diagnostics;
using Messaging.Observability.Library.ActivitySourceLogging;
using Wolverine;

namespace Messaging.Application.Services.Workers;

public sealed class OrderDomainProducerWorkerService(IActivitySourceFactory factory, MetricTestService metricsService, IMessageBus messageBus, ILogger<OrderDomainProducerWorkerService> logger)
{
    public async Task Run(CancellationToken cancellationToken)
    {
        var serviceName = nameof(MessagingProducerServiceHost);
        logger.LogInformation("Worker Service:{service} is starting.", serviceName);
        metricsService.Initialize("order-domain-messages-producer");
        while (!cancellationToken.IsCancellationRequested)
        {
            using var activity = factory.CreateActivity(serviceName, ActivityKind.Producer, "Creating Order Messages");

            {
                var orderId = Guid.NewGuid();
                await messageBus.PublishAsync(
                    new OrderCreated(orderId, "Donald Duck", 42, DateTimeOffset.UtcNow),
                    new DeliveryOptions
                    {
                    });

                await messageBus.PublishAsync(
                    new PaymentProcessed(Guid.CreateVersion7(DateTimeOffset.UtcNow), 42, "Paypal"),
                    new DeliveryOptions
                    {
                        Headers =
                        {
                            ["PaymentProvider"] = "Paypal",
                            ["ProcessedAt"] = DateTimeOffset.UtcNow.ToString("O")
                        }
                    });

                await messageBus.PublishAsync(
                    new OrderUpdated(orderId, "Doing Well", DateTimeOffset.UtcNow));
            }
            metricsService.IncrementTest();
            await Task.Delay(10, cancellationToken);
        }

    }
}