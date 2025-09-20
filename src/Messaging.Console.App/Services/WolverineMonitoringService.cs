using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Wolverine.Runtime;

namespace Messaging.Console.App.Services;

public class WolverineMonitoringService(IWolverineRuntime runtime, ILogger<WolverineMonitoringService> logger)
    : BackgroundService
{

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            // Check for failed messages, dead letters, etc.
            //var agents = runtime.Agents;
            //var activeAgents = agents.(a => a.Status == AgentStatus.Started);

            //logger.LogInformation("Wolverine agents running: {ActiveAgents}", activeAgents);

            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }
}