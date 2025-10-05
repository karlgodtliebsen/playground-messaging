using System.Collections.Concurrent;

namespace Messaging.RabbitMq.Library.LegacySupport;


//Mapping { "FullName, AssemblyName of the legacy type" to and from FullName of the serialization type that WolverineFx uses},
// No assembly name in included a Wolverine does not seem to support it

public class LegacyTypeMapper
{
    private readonly IDictionary<(string, string), string> map = new ConcurrentDictionary<(string, string), string>();
    private readonly IDictionary<string, (string, string)> reverseMap = new ConcurrentDictionary<string, (string, string)>();

    public void Register(string fromTypeName, string toTypeName)
    {
        var (typeName, assemblyName) = ShortSplitFqn(fromTypeName);
        map.TryAdd((typeName.Trim(), assemblyName.Trim()), toTypeName);
        reverseMap.TryAdd(toTypeName, (typeName.Trim(), assemblyName.Trim()));
    }
    public void Register(Type fromType, string toTypeName)
    {
        Register(fromType.AssemblyQualifiedName!, toTypeName);
    }
    public void Register<T>(string toTypeName)
    {
        Register(typeof(T), toTypeName);
    }

    public string MapFromLegacy(string typeName, string assemblyName)
    {
        map.TryGetValue((typeName, assemblyName), out var typeFullName);
        return typeFullName ?? typeName;
    }

    public (string typeName, string assemblyName) MapToLegacy(string fromTypeName)
    {
        if (reverseMap.TryGetValue(fromTypeName, out var result))
        {
            return result;
        }
        return (fromTypeName, "");
    }

    public (string typeName, string assemblyName) MapToLegacy(Type type)
    {
        return MapToLegacy(type.FullName!);
    }
    public (string typeName, string assemblyName) MapToLegacy<T>()
    {
        return MapToLegacy(typeof(T));
    }
    private static (string typeName, string assemblyName) ShortSplitFqn(string typeFullName)
    {
        ArgumentException.ThrowIfNullOrEmpty(typeFullName);
        var parts = typeFullName.Split(',', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length is not 2 and not 5) throw new InvalidOperationException($"{nameof(LegacyTypeMapper)} - Invalid {nameof(typeFullName)} - {typeFullName}");
        var typeName = parts[0];
        var assemblyName = string.Join(',', parts.Skip(1).Take(1).ToArray());
        return (typeName, assemblyName);
    }
}