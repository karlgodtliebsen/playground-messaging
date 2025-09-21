using Messaging.Console.App.Services;
using Messaging.EventHub.Library.Configuration;
using Messaging.Kafka.Library.Configuration;
using Messaging.RabbitMq.Library.Configuration;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using Wolverine;

namespace Messaging.Console.App.Configuration;

public static class HostBuilder
{
    public static IHost BuildKafkaProducerHost()
    {
        var builder = Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                services.AddLogging(loggingBuilder => { services.AddSerilogServices(loggingBuilder, context.Configuration); });
                services.AddHostedService<MessagingProducerServiceHost>();
            });


        builder.UseWolverine(KafkaProducerConfigurator.Build);
        var host = builder.Build();
        host.Services.SetupSerilog();
        return host;
    }

    public static IHost BuildKafkaConsumerHost()
    {
        var builder = Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                services.AddLogging(loggingBuilder => { services.AddSerilogServices(loggingBuilder, context.Configuration); });
                services.AddHostedService<MessagingConsumerServiceHost>();
            });


        builder.UseWolverine(KafkaConfigurator.Build);
        var host = builder.Build();
        host.Services.SetupSerilog();
        return host;
    }

    public static IHost BuildRabbitMqProducerHost()
    {
        var builder = Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                services
                    .AddProducerServices(context.Configuration)
                    .AddApplicationServices(context.Configuration)
                    .AddLogging(loggingBuilder => { services.AddSerilogServices(loggingBuilder, context.Configuration); })
                    .AddHostedService<MessagingDiagnosticsProducerServiceHost>()
                    .AddHostedService<QueueMonitoringService>();
            });

        builder.UseWolverine((opt) => RabbitMqConfigurationBuilder.BuildRabbitMqSetupUsingWolverine(opt));
        var host = builder.Build();
        host.Services.SetupSerilog();
        return host;
    }

    public static IHost BuildRabbitMqConsumerHost()
    {
        var builder = Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                services
                    .AddConsumerServices(context.Configuration)
                    .AddEventHubServices(context.Configuration)
                    .AddApplicationServices(context.Configuration)
                    .AddLogging(loggingBuilder => { services.AddSerilogServices(loggingBuilder, context.Configuration); })
                    .AddHostedService<MessagingConsumerServiceHost>()
                    .AddHostedService<QueueMonitoringService>();
            });
        builder.UseWolverine((opt) => RabbitMqConfigurationBuilder.BuildRabbitMqSetupUsingWolverine(opt));
        var host = builder.Build();
        host.Services.SetupSerilog();
        host.Services.SetupEventListener();
        return host;
    }
    public static IHost BuildRabbitMqCombinedHost()
    {
        var builder = Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                services
                    .AddProducerServices(context.Configuration)
                    .AddConsumerServices(context.Configuration)
                    .AddEventHubServices(context.Configuration)
                    .AddApplicationServices(context.Configuration)
                    .AddLogging(loggingBuilder => { services.AddSerilogServices(loggingBuilder, context.Configuration); })
                    .AddHostedService<MessagingConsumerServiceHost>()
                    .AddHostedService<MessagingDiagnosticsProducerServiceHost>()
                    .AddHostedService<QueueMonitoringService>()
                    ;
            });
        builder.UseWolverine((opt) => RabbitMqConfigurationBuilder.BuildRabbitMqSetupUsingWolverine(opt));
        var host = builder.Build();
        host.Services.SetupSerilog();
        host.Services.SetupEventListener();
        return host;
    }

    public static Task RunHostAsync(IHost host, string title, ILogger logger, CancellationToken cancellationToken)
    {
        try
        {
            return host.RunAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "Error starting Host for {title}", title);
            throw;
        }
    }

    public static Task RunHostsAsync(IEnumerable<IHost> hosts, string title, ILogger logger, CancellationToken cancellationToken)
    {
        try
        {
            IList<Task> tasks = new List<Task>();
            foreach (var host in hosts)
            {
                tasks.Add(host.RunAsync(cancellationToken));
            }

            return Task.WhenAll(tasks);
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "Error starting Hosts for {title}", title);
            throw;
        }
    }
}