using Messaging.Application.Services.Workers;
using Messaging.Hosting.Library;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Messaging.Application.Services.Hosts;

public sealed class SimpleMessagingProducerServiceHost(IServiceProvider serviceProvider, IHostApplicationLifetime lifetime, ILogger<SimpleMessagingProducerServiceHost> logger)
    : WaitingBackgroundService<SimpleMessagingProducerWorkerService>(serviceProvider, lifetime, logger)
{
    protected override async Task Run(SimpleMessagingProducerWorkerService worker, CancellationToken cancellationToken)
    {
        await worker.Run(cancellationToken);
    }
}