using MemoryMapped.Queue.Configuration;
using MemoryMapped.Queue.Serializers;

using Microsoft.Extensions.Options;

namespace MemoryMapped.Queue;

public class MemoryMappedQueue(IOptions<MemoryMappedOptions> mmOptions, IFastSerializer serializer) : IMemoryMappedQueue
{
    private readonly MemoryMappedQueueBuffer mmBuffer = new(mmOptions.Value.Name);

    public bool TryEnqueue<T>(T entry) where T : class
    {
        var payload = serializer.Serialize(entry);
        return (uint)payload.Length <= ushort.MaxValue && mmBuffer.TryEnqueue(payload);
    }

    public T? TryDequeue<T>() where T : class
    {
        var messageBytes = mmBuffer.TryDequeue();
        return messageBytes == Array.Empty<byte>() ? null : serializer.Deserialize<T>(messageBytes.AsSpan());
    }

    public IList<T> TryDequeueBatch<T>(int maxCount = 100) where T : class
    {
        var results = new List<T>(Math.Max(0, maxCount));
        for (var i = 0; i < maxCount; i++)
        {
            var entry = TryDequeue<T>();
            if (entry == null) break;
            results.Add(entry);
        }
        return results;
    }

    public MemoryMappedQueueStats GetStats()
    {
        return mmBuffer.GetStats();
    }

    public void Dispose()
    {
        mmBuffer.Dispose();
    }
}

