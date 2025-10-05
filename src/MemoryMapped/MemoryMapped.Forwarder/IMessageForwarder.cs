using Messaging.Domain.Library.DemoMessages;

namespace MemoryMapped.Forwarder;

public interface IMessageForwarder
{
    Task Initialize(CancellationToken cancellationToken);
    Task ForwardAsync(TextMessage entry, CancellationToken cancellationToken);
    Task ForwardBatchAsync(IEnumerable<TextMessage> entries, CancellationToken cancellationToken);
    Task<bool> TestConnectionAsync(CancellationToken cancellationToken);
}