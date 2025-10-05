using Messaging.Application.Services.Workers;
using Messaging.Hosting.Library;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Messaging.Application.Services.Hosts;

public sealed class MessagingConsumerServiceHost(IServiceProvider serviceProvider, IHostApplicationLifetime lifetime, ILogger<MessagingConsumerServiceHost> logger)
    : WaitingBackgroundService<MessagingConsumerWorkerService>(serviceProvider, lifetime, logger)
{
    protected override async Task Run(MessagingConsumerWorkerService worker, CancellationToken cancellationToken)
    {
        await worker.Run(cancellationToken);
    }
}