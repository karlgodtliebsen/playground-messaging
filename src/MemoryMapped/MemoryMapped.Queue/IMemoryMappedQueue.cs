namespace MemoryMapped.Queue;

public interface IMemoryMappedQueue : IDisposable
{
    bool TryEnqueue<T>(T entry);
    T? TryDequeue<T>();
    IList<T> TryDequeueBatch<T>(int maxCount = 100);
}

