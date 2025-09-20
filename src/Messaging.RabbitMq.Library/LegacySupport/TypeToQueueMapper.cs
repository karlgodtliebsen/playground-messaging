using System.Collections.Concurrent;

namespace Messaging.RabbitMq.Library.LegacySupport;

public class TypeToQueueMapper
{
    private readonly IDictionary<string, string> map = new ConcurrentDictionary<string, string>();

    public void Register(string typeName, string queueName)
    {
        map.TryAdd(typeName, queueName);
    }
    public void Register(Type type, string queueName)
    {
        map.TryAdd(type.FullName!, queueName);
    }
    public void Register<T>(string queueName)
    {
        map.TryAdd(typeof(T).FullName!, queueName);
    }

    public string? TryLookup(string typeName)
    {
        map.TryGetValue(typeName, out var queueName);
        return queueName;
    }
    public string? TryLookup(Type type)
    {
        map.TryGetValue(type.FullName!, out var queueName);
        return queueName;
    }
    public string? TryLookup<T>()
    {
        map.TryGetValue(typeof(T).FullName!, out var queueName);
        return queueName;
    }
}