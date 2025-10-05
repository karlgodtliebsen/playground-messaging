using MemoryMapped.Forwarder.Repositories;

using Messaging.Library;

namespace MemoryMapped.Forwarder;

public class MessageForwarder(IMessageRepository repository) : IMessageForwarder
{
    public async Task Initialize(CancellationToken cancellationToken)
    {
        await repository.CreateTable(cancellationToken);
    }

    public async Task ForwardAsync(IMessageBase entry, CancellationToken cancellationToken)
    {
        await repository.Add([entry], cancellationToken);
    }

    public async Task ForwardBatchAsync(IEnumerable<IMessageBase> entries, CancellationToken cancellationToken)
    {
        await repository.Add(entries, cancellationToken);
    }

    public async Task<bool> TestConnectionAsync(CancellationToken cancellationToken)
    {
        return await repository.TestConnection(cancellationToken);
    }
}