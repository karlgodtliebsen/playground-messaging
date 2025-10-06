namespace MemoryMapped.Queue;

public class MessageEnvelope
{
    public Guid Id { get; set; } = Guid.CreateVersion7(DateTimeOffset.UtcNow);
    public DateTimeOffset TimeStamp { get; set; } = DateTimeOffset.UtcNow;
    public Guid CorrelationId { get; set; } = Guid.CreateVersion7(DateTimeOffset.UtcNow);

    public string Message { get; set; } = null!;
    public string TypeFullName { get; set; } = null!;

}