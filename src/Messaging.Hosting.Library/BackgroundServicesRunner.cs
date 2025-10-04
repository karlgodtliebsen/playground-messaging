using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Messaging.Hosting.Library;

public static class BackgroundServicesRunner
{
    public static async Task RunAsync(IHost host, string title, ILogger logger, CancellationToken cancellationToken)
    {
        try
        {
            //await host.StartAsync(cancellationToken);
            await host.RunAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "Error starting Host named {title}", title);
            throw;
        }
    }

    public static async Task RunAsync(IEnumerable<IHost> hosts, string title, ILogger logger, CancellationToken cancellationToken)
    {
        try
        {
            IList<Task> tasks = new List<Task>();
            foreach (var host in hosts)
            {
                //var task = host.StartAsync(cancellationToken);
                //tasks.Add(task);
                tasks.Add(host.RunAsync(cancellationToken));
            }

            await Task.WhenAll(tasks);
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "Error starting Hosts named {title}", title);
            throw;
        }
    }
}