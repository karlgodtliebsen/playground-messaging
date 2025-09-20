using Messaging.Library;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Messaging.Console.App.Services;

public sealed class MessagingConsumerServiceHost(ISignalChannel channel, ILogger<MessagingMonitorServiceHost> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        var serviceName = nameof(MessagingMonitorServiceHost);
        logger.LogInformation("Background Service:{service} is running.", serviceName);

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await channel.Send("Alive", cancellationToken);
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