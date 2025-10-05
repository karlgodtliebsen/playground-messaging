using Messaging.Library;

namespace MemoryMapped.Forwarder;

public interface IMessageForwarder
{
    Task Initialize(CancellationToken cancellationToken);
    Task ForwardAsync(IMessageBase entry, CancellationToken cancellationToken);
    Task ForwardBatchAsync(IEnumerable<IMessageBase> entries, CancellationToken cancellationToken);
    Task<bool> TestConnectionAsync(CancellationToken cancellationToken);
}