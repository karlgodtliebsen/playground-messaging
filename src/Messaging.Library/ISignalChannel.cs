namespace Messaging.Library;

public interface ISignalChannel : IDisposable
{
    IDisposable Receive(string signalName, Func<CancellationToken, Task> handler);
    IDisposable Receive<T>(Func<T, CancellationToken, Task> handler);
    IDisposable Receive<T>(string signalName, Func<T, CancellationToken, Task> handler);
    IDisposable ReceiveAll(Action<string> handler);
    Task Send(string signal, CancellationToken cancellationToken = default);
    Task Send<T>(T data, CancellationToken cancellationToken = default);
    Task Send<T>(string signal, T data, CancellationToken cancellationToken = default);
}