using MemoryMapped.Forwarder.Configuration;
using MemoryMapped.Queue.Configuration;
using MemoryMapped.Repository.MsSql.Configuration;

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
                var configuration = context.Configuration;
                services
                    .AddKafkaApplicationServices(configuration)
                    .AddMemoryMappedQueueServices(configuration)
                    .AddHostedService<MessagingProducerServiceHost>()
                    .AddHostedService<SimpleMessagingProducerServiceHost>()
                    .AddHostedService<DiagnosticsMessagingProducerServiceHost>()
                    .AddLogging(loggingBuilder => { services.AddSerilogServices(loggingBuilder, configuration); });
            });


        builder.UseWolverine(KafkaConfigurationBuilder.BuildProducer);
        var host = builder.Build();
        host.Services.SetupSerilog();
        return host;
    }

    public static IHost BuildKafkaConsumerHost()
    {
        var builder = Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                var configuration = context.Configuration;
                services
                    .AddKafkaApplicationServices(configuration)

                    .AddMemoryMappedQueueServices(configuration)
                    .AddMsSqlServices(configuration)
                    .AddMessageForwarderServices(configuration)
                    .AddMessageForwarderHostServices(configuration)
                    .AddHostedService<MessagingConsumerServiceHost>()
                    .AddLogging(loggingBuilder => { services.AddSerilogServices(loggingBuilder, configuration); });
            });


        builder.UseWolverine(KafkaConfigurationBuilder.BuildConsumer);
        var host = builder.Build();
        host.Services.SetupSerilog();
        return host;
    }
    public static IHost BuildKafkaCombinedHost()
    {
        var builder = Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                var configuration = context.Configuration;
                services
                    .AddKafkaApplicationServices(configuration)
                    .AddMemoryMappedQueueServices(configuration)
                    .AddMessageForwarderServices(configuration)
                    .AddMessageForwarderHostServices(configuration)
                    .AddMsSqlServices(configuration)
                    .AddHostedService<MessagingProducerServiceHost>()
                    .AddHostedService<SimpleMessagingProducerServiceHost>()
                    .AddHostedService<DiagnosticsMessagingProducerServiceHost>()
                    .AddHostedService<MessagingConsumerServiceHost>()
                    .AddLogging(loggingBuilder => { services.AddSerilogServices(loggingBuilder, configuration); });
            });


        builder.UseWolverine(KafkaConfigurationBuilder.BuildCombined);
        var host = builder.Build();
        host.Services.SetupSerilog();

        return host;
    }


}