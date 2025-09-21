namespace Messaging.Library.EventHubChannel;

public interface IEventHub : IDisposable, IAsyncDisposable
{
    IDisposable Subscribe(string eventName, Func<CancellationToken, Task> handler);
    IDisposable Subscribe<T>(Func<T, CancellationToken, Task> handler);
    IDisposable Subscribe<T>(string eventName, Func<T, CancellationToken, Task> handler);
    IDisposable SubscribeToAll(Func<string, CancellationToken, Task> handler);
    Task Publish(string eventName, CancellationToken cancellationToken = default);
    Task Publish<T>(T data, CancellationToken cancellationToken = default);
    Task Publish<T>(string eventName, T data, CancellationToken cancellationToken = default);
    bool TryPublish(string eventName);
}