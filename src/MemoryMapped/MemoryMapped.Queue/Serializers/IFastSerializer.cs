namespace MemoryMapped.Queue.Serializers;

public interface IFastSerializer
{
    ReadOnlySpan<byte> Serialize<T>(T entry);
    T? Deserialize<T>(ReadOnlySpan<byte> buffer);
}