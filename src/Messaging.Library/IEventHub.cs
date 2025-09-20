namespace Messaging.Library;

public interface IEventHub : IDisposable
{
    IDisposable Subscribe(string signalName, Func<CancellationToken, Task> handler);
    IDisposable Subscribe<T>(Func<T, CancellationToken, Task> handler);
    IDisposable Subscribe<T>(string signalName, Func<T, CancellationToken, Task> handler);
    IDisposable SubscribeAll(Func<CancellationToken, Task> handler);
    Task Publish(string signal, CancellationToken cancellationToken = default);
    Task Publish<T>(T data, CancellationToken cancellationToken = default);
    Task Publish<T>(string signal, T data, CancellationToken cancellationToken = default);
}