using MemoryMapped.Queue;

using Microsoft.Extensions.Logging;

namespace MemoryMapped.Forwarder.WorkerServices;

public class MessageMemoryMappedShippingWorker(IMemoryMappedQueue memoryMappedQueue, IMessageForwarder forwarder, ILogger<MessageMemoryMappedShippingWorker> logger) : IMessageMemoryMappedShippingWorker
{
    private readonly int monitoringIntervalMs = 10;
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            await forwarder.Initialize(cancellationToken);
            while (!cancellationToken.IsCancellationRequested)
            {
                var entries = memoryMappedQueue.TryDequeueBatch();
                if (entries.Count > 0)
                {
                    await forwarder.ForwardBatchAsync(entries, cancellationToken);
                }
                await Task.Delay(monitoringIntervalMs, cancellationToken);
            }
        }
        catch (TaskCanceledException)
        {
            if (logger.IsEnabled(LogLevel.Trace)) logger.LogTrace("StartAsync method cancelled for Message Shipping Client.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An unexpected error occurred in StartAsync Message Shipping Client method.");
            throw;
        }
    }
}