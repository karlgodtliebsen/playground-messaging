namespace MemoryMapped.Forwarder;

public interface IMessageForwarder
{
    Task Initialize(CancellationToken cancellationToken);
    Task ForwardBatchAsync(IEnumerable<object> entries, CancellationToken cancellationToken);

    Task<bool> TestConnectionAsync(CancellationToken cancellationToken);
}