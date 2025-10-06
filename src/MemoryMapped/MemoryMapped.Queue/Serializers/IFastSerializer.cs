namespace MemoryMapped.Queue.Serializers;

public interface IFastSerializer
{
    ReadOnlySpan<byte> Serialize(object entry);

    object? Deserialize(ReadOnlySpan<byte> buffer);
}