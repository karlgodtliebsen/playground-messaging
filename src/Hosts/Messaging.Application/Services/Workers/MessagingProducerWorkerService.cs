using Messaging.Application.Services.Hosts;
using Messaging.Domain.Library.Orders;
using Messaging.Domain.Library.Payments;
using Microsoft.Extensions.Logging;
using Wolverine;

namespace Messaging.Application.Services.Workers;

public sealed class MessagingProducerWorkerService(IMessageBus messageBus, ILogger<MessagingProducerWorkerService> logger)
{
    public async Task Run(CancellationToken cancellationToken)
    {
        var serviceName = nameof(MessagingProducerServiceHost);
        logger.LogInformation("Worker Service:{service} is starting.", serviceName);
        while (!cancellationToken.IsCancellationRequested)
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

            await Task.Delay(10, cancellationToken);
        }

    }
}