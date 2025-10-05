using Messaging.Library;

namespace MemoryMapped.Forwarder.Repositories;

public interface IMessageRepository
{
    Task CreateTable(CancellationToken cancellationToken);
    Task Add(IEnumerable<IMessageBase> entries, CancellationToken cancellationToken);
    IAsyncEnumerable<IMessageBase> Find<T>(object? parameters, CancellationToken cancellationToken) where T : IMessageBase;

    Task<bool> TestConnection(CancellationToken cancellationToken);

}