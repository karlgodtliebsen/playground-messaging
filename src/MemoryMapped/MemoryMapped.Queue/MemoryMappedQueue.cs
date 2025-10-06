using MemoryMapped.Queue.Configuration;
using MemoryMapped.Queue.Serializers;

using Microsoft.Extensions.Options;

namespace MemoryMapped.Queue;

public class MemoryMappedQueue(IOptions<MemoryMappedOptions> mmOptions, IFastSerializer serializer) : IMemoryMappedQueue
{
    private readonly MemoryMappedQueueBuffer mmBuffer = new(mmOptions.Value.Name);

    public bool TryEnqueue(object entry)
    {
        var payload = serializer.Serialize(entry);
        return (uint)payload.Length <= ushort.MaxValue && mmBuffer.TryEnqueue(payload);
    }

    public object? TryDequeue()
    {
        var messageBytes = mmBuffer.TryDequeue();
        return messageBytes == Array.Empty<byte>() ? null : serializer.Deserialize(messageBytes.AsSpan());
    }

    public IList<object> TryDequeueBatch(int maxCount = 100)
    {
        var results = new List<object>(Math.Max(0, maxCount));
        for (var i = 0; i < maxCount; i++)
        {
            var entry = TryDequeue();
            if (entry is null) break;

            var t = entry.GetType();
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

