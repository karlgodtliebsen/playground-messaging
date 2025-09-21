using Messaging.Library.EventHubChannel;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Messaging.Console.App.Services;

public sealed class MessagingConsumerServiceHost(IEventHub eventHub, ILogger<MessagingConsumerServiceHost> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        var serviceName = nameof(MessagingConsumerServiceHost);
        logger.LogInformation("Background Service:{service} is running.", serviceName);

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await eventHub.Publish("Alive", cancellationToken);
                logger.LogInformation("ServiceHost Published Alive: {service}", serviceName);
                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
                //await Task.Delay(Timeout.Infinite, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            // This is expected when the service is stopping
            logger.LogInformation("Cancellation requested for service: {service}", serviceName);
        }

        logger.LogInformation("Stopping Consumer: {service}.", serviceName);
    }
}