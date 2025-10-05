using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Messaging.Hosting.Library;

public abstract class WaitingBackgroundService<T>(IServiceProvider serviceProvider, IHostApplicationLifetime lifetime, ILogger logger) : BackgroundService where T : notnull
{
    private const int ContinuousRetryIntervalMinutes = 1;
    private readonly TimeSpan continuousRetryTimeSpan = TimeSpan.FromMinutes(ContinuousRetryIntervalMinutes);

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        var serviceName = this.GetType().FullName!;
        logger.LogInformation("Waiting Background Service:{service} is starting.", serviceName);

        await WaitForApplicationStartedAsync(cancellationToken);

        var combinedPolicy = PolicyBuilder.CreateCombinedRetryPolicy(serviceName, continuousRetryTimeSpan, logger);
        await combinedPolicy.ExecuteAsync(async (ct) =>
        {
            var worker = serviceProvider.CreateScope().ServiceProvider.GetRequiredService<T>();
            await Run(worker, ct);
        }, cancellationToken);
    }

    protected abstract Task Run(T worker, CancellationToken cancellationToken);

    protected async Task WaitForApplicationStartedAsync(CancellationToken cancellationToken)
    {
        var tcs = new TaskCompletionSource();

        await using var registration = lifetime.ApplicationStarted.Register(() => tcs.SetResult());
        await using var cancellationRegistration = cancellationToken.Register(() => tcs.SetCanceled(cancellationToken));

        await tcs.Task;
    }
}