using Messaging.Console.App.Services.Workers;
using Messaging.Hosting.Library;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Messaging.Console.App.Services.Hosts;

public sealed class MessagingProducerServiceHost(IServiceProvider serviceProvider, IHostApplicationLifetime lifetime, ILogger<MessagingProducerServiceHost> logger)
    : WaitingBackgroundService<MessagingProducerWorkerService>(serviceProvider, lifetime, logger)
{
    protected override async Task Run(MessagingProducerWorkerService worker, CancellationToken cancellationToken)
    {
        await worker.Run(cancellationToken);
    }
}