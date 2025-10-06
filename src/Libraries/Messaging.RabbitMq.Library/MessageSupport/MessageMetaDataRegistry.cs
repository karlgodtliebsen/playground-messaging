using System.Collections.Concurrent;

namespace Messaging.RabbitMq.Library.MessageSupport;

public class MessageMetaDataRegistry
{
    private readonly IDictionary<Type, IMessageTypeMap> registry = new ConcurrentDictionary<Type, IMessageTypeMap>();

    public IMessageTypeMap Register<T>(string exchangeName, string routingKey, string bindingPattern, string? queueName = null)
    {
        return Register(typeof(T), exchangeName, routingKey, bindingPattern, queueName);
    }

    public IMessageTypeMap Register(Type type, string exchangeName, string routingKey, string bindingPattern, string? queueName = null)
    {
        IMessageTypeMap map = new MessageTypeMap(type, exchangeName, routingKey, bindingPattern, queueName);
        registry.Add(type, map);
        return map;
    }

    public IMessageTypeMap? TryLookup(Type type)
    {
        registry.TryGetValue(type, out var messageMap);
        return messageMap;
    }

    public IMessageTypeMap? TryLookup<T>()
    {
        registry.TryGetValue(typeof(T), out var messageMap);
        return messageMap;
    }

    public IEnumerable<IMessageTypeMap> Registries => registry.Values;
}