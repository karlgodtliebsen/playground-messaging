using Messaging.Application.Services.Workers;
using Messaging.Hosting.Library;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Messaging.Application.Services.Hosts;

public sealed class DiagnosticsMessagingProducerServiceHost(IServiceProvider serviceProvider, IHostApplicationLifetime lifetime, ILogger<DiagnosticsMessagingProducerServiceHost> logger)
    : WaitingBackgroundService<DiagnosticsMessagingProducerWorkerService>(serviceProvider, lifetime, logger)
{
    protected override async Task Run(DiagnosticsMessagingProducerWorkerService worker, CancellationToken cancellationToken)
    {
        await worker.Run(cancellationToken);
    }
}