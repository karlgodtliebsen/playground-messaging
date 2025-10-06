namespace MemoryMapped.Forwarder.WorkerServices;

public interface IMessageMemoryMappedShippingWorker
{
    Task StartAsync(CancellationToken cancellationToken);
}