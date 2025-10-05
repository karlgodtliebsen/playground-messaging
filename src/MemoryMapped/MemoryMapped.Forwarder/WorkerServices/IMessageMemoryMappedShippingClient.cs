namespace MemoryMapped.Forwarder.WorkerServices;

public interface IMessageMemoryMappedShippingClient
{
    Task StartAsync(CancellationToken cancellationToken);
}