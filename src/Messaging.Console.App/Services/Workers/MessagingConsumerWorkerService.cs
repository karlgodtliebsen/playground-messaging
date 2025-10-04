using Messaging.EventHub.Library;
using Microsoft.Extensions.Logging;

namespace Messaging.Console.App.Services.Workers;

public sealed class MessagingConsumerWorkerService(IEventHub eventHub, ILogger<MessagingConsumerWorkerService> logger)
{
    public async Task Run(CancellationToken cancellationToken)
    {
        var serviceName = nameof(MessagingConsumerWorkerService);
        logger.LogInformation("Worker Service:{service} is starting.", serviceName);
        while (!cancellationToken.IsCancellationRequested)
        {
            await eventHub.Publish("Alive", cancellationToken);
            //logger.LogInformation("ServiceHost Published Alive: {service}", serviceName);
            await Task.Delay(10, cancellationToken);

            //await Task.Delay(Timeout.Infinite, cancellationToken);
        }

    }
}