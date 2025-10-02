namespace Messaging.EventHub.Library.EventHubs;

public static class EventHubTestExtensions
{
    public static async Task PublishAndWaitAsync(
        this IEventHub hub,
        string eventName,
        CancellationToken ct = default,
        TimeSpan? timeout = null)
    {
        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var id = Guid.NewGuid().ToString("N");
        // Use a unique event name to avoid counting others:
        var barrierName = $"{eventName}::{id}";

        using var sub = hub.SubscribeToAll(async (name, token) =>
        {
            if (name == barrierName)
                tcs.TrySetResult();
            await Task.CompletedTask;
        });

        await hub.Publish(eventName, ct);
        await hub.Publish(barrierName, ct); // barrier processed after previous messages

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(timeout ?? TimeSpan.FromSeconds(5));
        await tcs.Task.WaitAsync(cts.Token);
    }
}