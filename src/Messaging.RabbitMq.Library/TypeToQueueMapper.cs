using System.Collections.Concurrent;

namespace Messaging.RabbitMq.Library;

public class TypeToQueueMapper
{
    private IDictionary<string, string> map = new ConcurrentDictionary<string, string>();

    public void Register(string key, string typeName)
    {
        map.TryAdd(key, typeName);
    }

    public string? GetQueueName(string typeName)
    {
        map.TryGetValue(typeName, out var queueName);
        return queueName;
    }

}