using Messaging.Application.Services;
using Messaging.Application.Services.Hosts;
using Messaging.EventHub.Library.Configuration;
using Messaging.RabbitMq.Library.Configuration;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Wolverine;

namespace Messaging.Application.Configuration.RabbitMqSupport;

public static class RabbitMqBuilder
{
    public static IHost BuildRabbitMqProducerHost()
    {
        var builder = Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                var configuration = context.Configuration;
                services
                    .AddProducerServices(configuration)
                    .AddApplicationServices(configuration)
                    .AddLogging(loggingBuilder => { services.AddSerilogServices(loggingBuilder, configuration); })
                    .AddHostedService<DiagnosticsMessagingProducerServiceHost>()
                    //.AddHostedService<RabbitMqQueueMonitoringService>()
                    ;
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
                var configuration = context.Configuration;
                services
                    .AddConsumerServices(configuration)
                    .AddEventHubServices(configuration)
                    .AddApplicationServices(configuration)
                    .AddLogging(loggingBuilder => { services.AddSerilogServices(loggingBuilder, configuration); })
                    .AddHostedService<MessagingConsumerServiceHost>()
                    //.AddHostedService<RabbitMqQueueMonitoringService>()
                    ;
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
                var configuration = context.Configuration;
                services
                    .AddProducerServices(configuration)
                    .AddConsumerServices(configuration)
                    .AddEventHubServices(configuration)
                    .AddApplicationServices(configuration)
                    .AddLogging(loggingBuilder => { services.AddSerilogServices(loggingBuilder, configuration); })
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