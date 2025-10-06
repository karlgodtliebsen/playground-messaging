using MemoryPack;

using System.Buffers;
using System.Text;

namespace MemoryMapped.Queue.Serializers;

public class FastMemoryPackSerializer : IFastSerializer
{
    //look at MemoryPackSerializerOptions
    public ReadOnlySpan<byte> Serialize(object entry)// where T : class
    {
        var t = entry.GetType();
        var typeName = t.AssemblyQualifiedName!;
        var typeNameBytes = Encoding.UTF8.GetBytes(typeName);
        using var writer = new PooledBufferWriter();    //System.Buffer.ArrayBufferWriter<byte>();

        // Write type name length (4 bytes)
        writer.Write(BitConverter.GetBytes(typeNameBytes.Length));
        // Write type name
        writer.Write(typeNameBytes);
        MemoryPackSerializer.Serialize(writer, entry);
        return writer.WrittenSpan;
    }

    public T? Deserialize<T>(ReadOnlySpan<byte> buffer)// where T : class
    {
        return MemoryPackSerializer.Deserialize<T>(buffer);
    }
    public object? Deserialize(ReadOnlySpan<byte> buffer)
    {
        if (buffer.IsEmpty) return null;

        // Read type name length
        var typeNameLength = BitConverter.ToInt32(buffer.Slice(0, 4));

        // Read type name
        var typeName = Encoding.UTF8.GetString(buffer.Slice(4, typeNameLength));

        // Get the type
        var type = Type.GetType(typeName);
        if (type == null)
            throw new InvalidOperationException($"Type not found: {typeName}");

        // Deserialize payload
        var payload = buffer.Slice(4 + typeNameLength);
        object? value = null;
        var result = MemoryPackSerializer.Deserialize(type, payload, ref value);
        if (result == 0) return null;
        return value;
    }

}