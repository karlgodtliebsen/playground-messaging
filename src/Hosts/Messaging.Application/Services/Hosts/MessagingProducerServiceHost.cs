using Messaging.Application.Services.Workers;
using Messaging.Hosting.Library;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Messaging.Application.Services.Hosts;

public sealed class MessagingProducerServiceHost(IServiceProvider serviceProvider, IHostApplicationLifetime lifetime, ILogger<MessagingProducerServiceHost> logger)
    : WaitingBackgroundService<OrderDomainProducerWorkerService>(serviceProvider, lifetime, logger)
{
    protected override async Task Run(OrderDomainProducerWorkerService worker, CancellationToken cancellationToken)
    {
        await worker.Run(cancellationToken);
    }
}