namespace Messaging.Library;

public interface ISignalChannel : IDisposable
{
    IDisposable Subscribe(string signalName, Func<CancellationToken, Task> handler);
    IDisposable Subscribe<T>(Func<T, CancellationToken, Task> handler);
    IDisposable Subscribe<T>(string signalName, Func<T, CancellationToken, Task> handler);
    IDisposable SubscribeAll(Action<string> handler);
    Task Publish(string signal, CancellationToken cancellationToken = default);
    Task Publish<T>(T data, CancellationToken cancellationToken = default);
    Task Publish<T>(string signal, T data, CancellationToken cancellationToken = default);
}