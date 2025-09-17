using Messaging.Library.Orders;
using Messaging.Library.Payments;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Wolverine;

namespace Messaging.Console.App.Services;

public sealed class MessagingProducerServiceHost(IMessageBus messageBus, ILogger<MessagingProducerServiceHost> logger) : BackgroundService
{
    private const int ContinuousRetryIntervalMinutes = 1;
    private readonly TimeSpan continuousRetryTimeSpan = TimeSpan.FromMinutes(ContinuousRetryIntervalMinutes);

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        var serviceName = nameof(MessagingProducerServiceHost);
        logger.LogInformation("Background Service:{service} is running.", serviceName);
        var combinedPolicy = HostingPolicyBuilder.CreateCombinedRetryPolicy(serviceName, continuousRetryTimeSpan, logger);

        await combinedPolicy.ExecuteAsync(async (ct) =>
        {
            while (!ct.IsCancellationRequested)
            {
                var orderId = Guid.NewGuid();
                await messageBus.PublishAsync(
                    new OrderCreated(orderId, "Donald Duck", 42, DateTimeOffset.UtcNow),
                    new DeliveryOptions
                    {
                        Headers =
                        {
                            ["CreatedBy"] = "Message Producer Console App",
                            ["Timestamp"] = DateTimeOffset.UtcNow.ToString("O"),
                            ["CorrelationId"] = Guid.CreateVersion7(DateTimeOffset.UtcNow).ToString()
                        }
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

                await Task.Delay(10, ct);
            }
        }, cancellationToken);
    }
}