using Messaging.Application.Services.Hosts;
using Messaging.Domain.Library.Orders;
using Messaging.Domain.Library.SimpleMessages;
using Microsoft.Extensions.Logging;
using Wolverine;

namespace Messaging.Application.Services.Workers;

public sealed class SimpleMessagingProducerWorkerService(IMessageBus messageBus, ILogger<SimpleMessagingProducerWorkerService> logger)
{
    public async Task Run(CancellationToken cancellationToken)
    {
        var serviceName = nameof(MessagingProducerServiceHost);
        logger.LogInformation("Worker Service:{service} is starting.", serviceName);
        while (!cancellationToken.IsCancellationRequested)
        {
            var orderId = Guid.NewGuid();
            await messageBus.PublishAsync(
                new InformationMessage(Guid.CreateVersion7(DateTimeOffset.UtcNow).ToString("N"), "InformationMessage from Paypal"),
                new DeliveryOptions
                {
                });

            await messageBus.PublishAsync(
                new CreateMessage()
                {
                    SenderId = Guid.CreateVersion7(DateTimeOffset.UtcNow),
                    Content = "Message from Paypal"
                },
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