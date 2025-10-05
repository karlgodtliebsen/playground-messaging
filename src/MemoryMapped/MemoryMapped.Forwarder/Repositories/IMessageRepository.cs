using Messaging.Domain.Library.DemoMessages;

namespace MemoryMapped.Forwarder.Repositories;

public interface IMessageRepository
{
    Task CreateTable(CancellationToken cancellationToken);
    Task Add(IEnumerable<TextMessage> entries, CancellationToken cancellationToken);
    IAsyncEnumerable<TextMessage> Find(object? parameters, CancellationToken cancellationToken);

    Task<bool> TestConnection(CancellationToken cancellationToken);

}