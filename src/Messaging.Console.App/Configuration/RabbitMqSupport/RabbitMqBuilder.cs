using Messaging.Console.App.Services;
using Messaging.Console.App.Services.Hosts;
using Messaging.EventHub.Library.Configuration;
using Messaging.RabbitMq.Library.Configuration;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Wolverine;

namespace Messaging.Console.App.Configuration.RabbitMqSupport;

public static class RabbitMqBuilder
{
    public static IHost BuildRabbitMqProducerHost()
    {
        var builder = Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                services
                    .AddProducerServices(context.Configuration)
                    .AddApplicationServices(context.Configuration)
                    .AddLogging(loggingBuilder => { services.AddSerilogServices(loggingBuilder, context.Configuration); })
                    .AddHostedService<DiagnosticsMessagingProducerServiceHost>()
                    .AddHostedService<RabbitMqQueueMonitoringService>();
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
                    .AddHostedService<RabbitMqQueueMonitoringService>();
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
                    .AddHostedService<DiagnosticsMessagingProducerServiceHost>()
                    .AddHostedService<RabbitMqQueueMonitoringService>()
                    ;
            });
        builder.UseWolverine((opt) => RabbitMqConfigurationBuilder.BuildRabbitMqSetupUsingWolverine(opt));
        var host = builder.Build();
        host.Services.SetupSerilog();
        host.Services.SetupEventListener();
        return host;
    }
}