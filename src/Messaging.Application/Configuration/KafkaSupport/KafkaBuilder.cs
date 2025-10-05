using Messaging.Application.Services.Hosts;
using Messaging.Kafka.Library.Configuration;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Wolverine;

namespace Messaging.Application.Configuration.KafkaSupport;

public static class KafkaBuilder
{
    public static IHost BuildKafkaProducerHost()
    {
        var builder = Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                services
                    .AddKafkaApplicationServices(context.Configuration)
                    .AddHostedService<MessagingProducerServiceHost>()
                    .AddHostedService<SimpleMessagingProducerServiceHost>()
                    .AddHostedService<DiagnosticsMessagingProducerServiceHost>()
                    .AddLogging(loggingBuilder => { services.AddSerilogServices(loggingBuilder, context.Configuration); });
            });


        builder.UseWolverine(KafkaConfigurator.BuildProducer);
        var host = builder.Build();
        host.Services.SetupSerilog();
        return host;
    }

    public static IHost BuildKafkaConsumerHost()
    {
        var builder = Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                services
                    .AddKafkaApplicationServices(context.Configuration)
                    .AddHostedService<MessagingConsumerServiceHost>()
                    .AddLogging(loggingBuilder => { services.AddSerilogServices(loggingBuilder, context.Configuration); });
            });


        builder.UseWolverine(KafkaConfigurator.BuildConsumer);
        var host = builder.Build();
        host.Services.SetupSerilog();
        return host;
    }
    public static IHost BuildKafkaCombinedHost()
    {
        var builder = Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                services
                    .AddKafkaApplicationServices(context.Configuration)
                    .AddHostedService<MessagingProducerServiceHost>()
                    .AddHostedService<SimpleMessagingProducerServiceHost>()
                    .AddHostedService<DiagnosticsMessagingProducerServiceHost>()
                    .AddHostedService<MessagingConsumerServiceHost>()
                    .AddLogging(loggingBuilder => { services.AddSerilogServices(loggingBuilder, context.Configuration); });
            });


        builder.UseWolverine(KafkaConfigurator.BuildCombined);
        var host = builder.Build();
        host.Services.SetupSerilog();

        return host;
    }


}