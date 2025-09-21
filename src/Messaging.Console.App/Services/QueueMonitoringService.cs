using Messaging.RabbitMq.Library.Configuration;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using RabbitMQ.Client;

namespace Messaging.Console.App.Services;

public class QueueMonitoringService(

    [FromKeyedServices("monitor")] IOptions<string[]> queueNamesOptions,
    IOptions<RabbitMqOptions> options, ILogger<QueueMonitoringService> logger)
    : BackgroundService
{
    private readonly TimeSpan checkInterval = TimeSpan.FromMinutes(1);
    private const int ContinuousRetryIntervalMinutes = 1;
    private readonly TimeSpan continuousRetryTimeSpan = TimeSpan.FromMinutes(ContinuousRetryIntervalMinutes);
    private readonly RabbitMqOptions configuration = options.Value;
    private readonly string[] queueNames = queueNamesOptions.Value;
    private string? connectionString;
    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        connectionString = BuildConnectionString();

        var serviceName = nameof(QueueMonitoringService);
        logger.LogInformation("Background Service:{service} is running.", serviceName);
        var combinedPolicy = PolicyBuilder.CreateCombinedRetryPolicy(serviceName, continuousRetryTimeSpan, logger);
        await combinedPolicy.ExecuteAsync(async (ct) =>
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await MonitorQueues();
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error monitoring queues");
                }

                await Task.Delay(checkInterval, cancellationToken);
            }

        }, cancellationToken);

    }
    private string BuildConnectionString()
    {
        var uriBuilder = new UriBuilder
        {
            Scheme = "amqp",
            Host = configuration.HostName,
            Port = configuration.Port,
            Path = configuration.VirtualHost,
            UserName = configuration.UserName,
            Password = configuration.Password
        };

        // Add query parameters for timeouts
        var queryParams = new List<string>();

        if (configuration.Heartbeat != ConnectionFactory.DefaultHeartbeat)
        {
            queryParams.Add($"heartbeat={configuration.Heartbeat.TotalSeconds}");
        }

        if (configuration.DefaultConnectionTimeout != ConnectionFactory.DefaultConnectionTimeout)
        {
            queryParams.Add($"connection_timeout={configuration.DefaultConnectionTimeout.TotalMilliseconds}");
        }

        if (queryParams.Any())
        {
            uriBuilder.Query = string.Join("&", queryParams);
        }

        return uriBuilder.ToString();
    }

    private async Task MonitorQueues()
    {
        if (connectionString is null)
        {
            logger.LogWarning("MonitorQueues: A valid ConnectionString was not Provided. Monitoring not possible");
            return;
        }
        var factory = new ConnectionFactory { Uri = new Uri(connectionString) };

        await using var connection = await factory.CreateConnectionAsync();
        await using var channel = await connection.CreateChannelAsync();
        foreach (var queueName in queueNames)
        {
            try
            {
                var queueInfo = await channel.QueueDeclarePassiveAsync(queueName);
                var messageCount = queueInfo.MessageCount;
                var consumerCount = queueInfo.ConsumerCount;

                logger.LogInformation("Queue {QueueName}: {MessageCount} messages, {ConsumerCount} consumers", queueName, messageCount, consumerCount);

                // Alert if queue is backing up
                if (messageCount > 1000) // Your threshold
                {
                    logger.LogWarning("Queue {QueueName} has {MessageCount} messages - potential backup!", queueName, messageCount);
                }
            }
            catch (RabbitMQ.Client.Exceptions.OperationInterruptedException ex)
                when (ex.ShutdownReason?.ReplyCode == 404)
            {
                logger.LogWarning("Queue {queueName} does not exist", queueName);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to check queue {QueueName}", queueName);
            }
        }
    }
}