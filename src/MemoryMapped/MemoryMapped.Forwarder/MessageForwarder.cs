using MemoryMapped.Forwarder.Repositories;

using Messaging.Domain.Library.DemoMessages;

namespace MemoryMapped.Forwarder;

public class MessageForwarder(IMessageRepository repository) : IMessageForwarder
{
    public async Task Initialize(CancellationToken cancellationToken)
    {
        await repository.CreateTable(cancellationToken);
    }

    public async Task ForwardAsync(TextMessage entry, CancellationToken cancellationToken)
    {
        await repository.Add([entry], cancellationToken);
    }

    public async Task ForwardBatchAsync(IEnumerable<TextMessage> entries, CancellationToken cancellationToken)
    {
        await repository.Add(entries, cancellationToken);
    }

    public async Task<bool> TestConnectionAsync(CancellationToken cancellationToken)
    {
        return await repository.TestConnection(cancellationToken);
    }
}