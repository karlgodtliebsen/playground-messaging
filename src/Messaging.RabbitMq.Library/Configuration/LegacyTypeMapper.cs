using System.Collections.Concurrent;

namespace Messaging.RabbitMq.Library.Configuration;


//Mapping { "FullName of the legacy type", FullName of the new type },
// No assembly name in included a Wolverine does not seem to support it

public class LegacyTypeMapper
{
    private readonly IDictionary<string, string> map = new ConcurrentDictionary<string, string>();

    public void Register(string fromTypeName, string toTypeName)
    {
        map.TryAdd(fromTypeName, toTypeName);
    }
    public void Register(Type fromType, string toTypeName)
    {
        map.TryAdd(fromType.FullName!, toTypeName);
    }
    public void Register<T>(string toTypeName)
    {
        map.TryAdd(typeof(T).FullName!, toTypeName);
    }

    public string MapFrom(string fromTypeName)
    {
        map.TryGetValue(fromTypeName, out var typeFullName);
        return typeFullName ?? fromTypeName;
    }
    public string MapFrom(Type fromType)
    {
        map.TryGetValue(fromType.FullName!, out var typeFullName);
        return typeFullName ?? fromType.FullName!;
    }
    public string MapFrom<T>()
    {
        map.TryGetValue(typeof(T).FullName!, out var typeFullName);
        return typeFullName ?? typeof(T).FullName!;
    }
}