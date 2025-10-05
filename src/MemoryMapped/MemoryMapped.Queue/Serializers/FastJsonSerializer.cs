using System.Buffers;
using System.Text;
using System.Text.Json;

namespace MemoryMapped.Queue.Serializers;

public class FastJsonSerializer : IFastSerializer
{
    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

    public ReadOnlySpan<byte> Serialize<T>(T entry)// where T : class
    {
        var t = typeof(T);
        var typeName = t.AssemblyQualifiedName!;
        var typeNameBytes = Encoding.UTF8.GetBytes(typeName);
        using var writer = new PooledBufferWriter();

        // Write type name length (4 bytes)
        writer.Write(BitConverter.GetBytes(typeNameBytes.Length));

        // Write type name
        writer.Write(typeNameBytes);

        // Write JSON payload
        using var jsonWriter = new Utf8JsonWriter(writer);
        JsonSerializer.Serialize(jsonWriter, entry, JsonOpts);
        jsonWriter.Flush();

        return writer.WrittenSpan;
    }

    public T? Deserialize<T>(ReadOnlySpan<byte> buffer)
    {
        if (buffer.IsEmpty) return default;

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
        return (T)JsonSerializer.Deserialize(payload, type, JsonOpts);
    }
}

//public class TypeRegistry
//{
//    private static readonly Dictionary<int, Type> _idToType = new();
//    private static readonly Dictionary<Type, int> _typeToId = new();

//    static TypeRegistry()
//    {
//        Register(1, typeof(OrderCreated));
//        Register(2, typeof(OrderUpdated));
//        // ... register all your types
//    }

//    private static void Register(int id, Type type)
//    {
//        _idToType[id] = type;
//        _typeToId[type] = id;
//    }

//    public static int GetId(Type type) => _typeToId[type];
//    public static Type GetType(int id) => _idToType[id];
//}

//public ReadOnlySpan<byte> Serialize<T>(T entry) where T : class
//{
//    var typeId = TypeRegistry.GetId(typeof(T));

//    using var writer = new PooledBufferWriter();

//    // Write type ID (2 bytes for up to 65k types)
//    writer.Write(BitConverter.GetBytes((ushort)typeId));

//    // Write JSON payload
//    using var jsonWriter = new Utf8JsonWriter(writer);
//    JsonSerializer.Serialize(jsonWriter, entry, JsonOpts);
//    jsonWriter.Flush();

//    return writer.WrittenSpan;
//}

//public object? Deserialize(ReadOnlySpan<byte> buffer)
//{
//    if (buffer.IsEmpty) return null;

//    var typeId = BitConverter.ToUInt16(buffer.Slice(0, 2));
//    var type = TypeRegistry.GetType(typeId);
//    var payload = buffer.Slice(2);

//    return JsonSerializer.Deserialize(payload, type, JsonOpts);
//}