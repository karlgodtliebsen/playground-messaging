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

                await Task.Delay(10, ct);
            }
        }, cancellationToken);
    }
}