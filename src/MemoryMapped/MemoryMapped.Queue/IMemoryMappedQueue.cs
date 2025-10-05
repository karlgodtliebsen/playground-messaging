namespace MemoryMapped.Queue;

public interface IMemoryMappedQueue : IDisposable
{
    bool TryEnqueue<T>(T entry) where T : class;
    T? TryDequeue<T>() where T : class;
    IList<T> TryDequeueBatch<T>(int maxCount = 100) where T : class;
}

