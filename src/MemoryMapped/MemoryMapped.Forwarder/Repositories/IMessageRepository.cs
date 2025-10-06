using MemoryMapped.Queue;

namespace MemoryMapped.Forwarder.Repositories;

public interface IMessageRepository
{
    Task CreateTable(CancellationToken cancellationToken);
    Task Add(IEnumerable<MessageEnvelope> entries, CancellationToken cancellationToken);
    IAsyncEnumerable<MessageEnvelope> Find(object? parameters, CancellationToken cancellationToken);

    Task<bool> TestConnection(CancellationToken cancellationToken);

}