using Messaging.EventHub.Library;

using Microsoft.Extensions.Logging;

namespace Messaging.Application.Services.Workers;

public sealed class MessagingConsumerWorkerService(IEventHub eventHub, ILogger<MessagingConsumerWorkerService> logger)
{
    public async Task Run(CancellationToken cancellationToken)
    {
        var serviceName = nameof(MessagingConsumerWorkerService) + "-" + Ulid.NewUlid(DateTimeOffset.UtcNow).ToString();
        logger.LogInformation("Consumer Worker Service: {service} is starting.", serviceName);
        while (!cancellationToken.IsCancellationRequested)
        {
            //await eventHub.Publish("Alive", cancellationToken);
            await eventHub.Publish("Alive", serviceName, cancellationToken);
            //logger.LogInformation("ServiceHost Published Alive: {service}", serviceName);
            await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);
            //await Task.Delay(Timeout.Infinite, cancellationToken);
        }

    }
}