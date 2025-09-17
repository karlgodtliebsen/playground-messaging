using Messaging.Library.Orders;
using Messaging.RabbitMq.Library;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Wolverine;

namespace Messaging.Console.App.Services;

public sealed class MessagingDiagnosticsProducerServiceHost(IMessageBus messageBus, ILogger<MessagingDiagnosticsProducerServiceHost> logger) : BackgroundService
{
    private const int ContinuousRetryIntervalMinutes = 1;
    private readonly TimeSpan continuousRetryTimeSpan = TimeSpan.FromMinutes(ContinuousRetryIntervalMinutes);

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        var serviceName = nameof(MessagingDiagnosticsProducerServiceHost);
        logger.LogInformation("Background Service:{service} is running.", serviceName);
        var combinedPolicy = HostingPolicyBuilder.CreateCombinedRetryPolicy(serviceName, continuousRetryTimeSpan, logger);

        await combinedPolicy.ExecuteAsync(async (ct) =>
        {
            while (!ct.IsCancellationRequested)
            {
                var orderId = Guid.NewGuid();
                await messageBus.PublishAsync(
                    new TextMessage("the host", "the app", "hellow world", DateTimeOffset.UtcNow),
                    new DeliveryOptions
                    {
                        Headers =
                        {
                            ["SendBy"] = "TextMessage Producer Console App",
                            ["Timestamp"] = DateTimeOffset.UtcNow.ToString("O"),
                            ["CorrelationId"] = Guid.CreateVersion7(DateTimeOffset.UtcNow).ToString()
                        }
                    });

                await messageBus.PublishAsync(
                    new PingMessage("the host", "the app", DateTimeOffset.UtcNow),
                    new DeliveryOptions
                    {
                        Headers =
                        {
                            ["Application"] = "TextMessage Producer Console App",
                            ["ProcessedAt"] = DateTimeOffset.UtcNow.ToString("O")
                        }
                    });

                await Task.Delay(10, ct);
            }
        }, cancellationToken);
    }
}