namespace MemoryMapped.Queue;

public interface IMemoryMappedQueue : IDisposable
{
    bool TryEnqueue(object entry);
    object? TryDequeue();
    IList<object> TryDequeueBatch(int maxCount = 100);
}

