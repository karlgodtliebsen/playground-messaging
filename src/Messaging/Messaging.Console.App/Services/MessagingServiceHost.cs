using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Messaging.Console.App.Services;

public sealed class MessagingServiceHost(ILogger<MessagingServiceHost> logger) : BackgroundService
{
    private Task? runningTask;

    private const int ContinuousRetryIntervalMinutes = 1;
    private readonly TimeSpan continuousRetryTimeSpan = TimeSpan.FromMinutes(ContinuousRetryIntervalMinutes);

    protected override Task ExecuteAsync(CancellationToken cancellationToken)
    {
        var serviceName = nameof(MessagingServiceHost);
        logger.LogInformation("Background Service:{service} is running.", serviceName);

        var combinedPolicy = HostingPolicyBuilder.CreateCombinedRetryPolicy(serviceName, continuousRetryTimeSpan, logger);

        runningTask = combinedPolicy.ExecuteAsync(async (ct) =>
        {
            //await workerService.ExecuteAsync(ct);

            //TODO: add the service call here

            await Task.Delay(1, ct);
        }, cancellationToken);

        return runningTask;
    }


    public override void Dispose()
    {
        if (runningTask is not null)
        {
            if (runningTask.IsCompleted)
            {
                runningTask.Dispose();
            }

            runningTask = null;
        }

        //workerService.Dispose();
        base.Dispose();
    }
}