using Messaging.Application.Services.Hosts;
using Messaging.Domain.Library.DemoMessages;
using Microsoft.Extensions.Logging;
using Wolverine;

namespace Messaging.Application.Services.Workers;

public sealed class DiagnosticsMessagingProducerWorkerService(IMessageBus messageBus, ILogger<MessagingProducerWorkerService> logger)
{
    public async Task Run(CancellationToken cancellationToken)
    {
        var serviceName = nameof(DiagnosticsMessagingProducerServiceHost);
        logger.LogInformation("Worker Service:{service} is starting.", serviceName);
        while (!cancellationToken.IsCancellationRequested)
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

            await Task.Delay(10, cancellationToken);
        }
    }
}