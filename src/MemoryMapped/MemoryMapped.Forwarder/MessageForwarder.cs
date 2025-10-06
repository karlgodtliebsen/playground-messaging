using MemoryMapped.Forwarder.Repositories;
using MemoryMapped.Queue;

namespace MemoryMapped.Forwarder;

public class MessageForwarder(IMessageRepository repository) : IMessageForwarder
{
    public async Task Initialize(CancellationToken cancellationToken)
    {
        await repository.CreateTable(cancellationToken);
    }

    public async Task ForwardBatchAsync(IEnumerable<object> entries, CancellationToken cancellationToken)
    {
        var objects = entries.Select(e => new MessageEnvelope()
        {
            Message = System.Text.Json.JsonSerializer.Serialize(e),
            TypeFullName = e.GetType().AssemblyQualifiedName ?? ""
        });
        await repository.Add(objects, cancellationToken);
    }

    public async Task<bool> TestConnectionAsync(CancellationToken cancellationToken)
    {
        return await repository.TestConnection(cancellationToken);
    }
}