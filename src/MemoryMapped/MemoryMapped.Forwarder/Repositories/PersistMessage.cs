namespace MemoryMapped.Forwarder.Repositories;

public class PersistMessage
{
    public Guid Id { get; set; }
    public DateTimeOffset TimeStamp { get; set; }
    public Guid CorrelationId { get; set; }
    public string TypeFullName { get; set; } = null!;
    public string Message { get; set; } = null!;

}