using Messaging.Console.App.Services;
using Messaging.Kafka.Library.Configuration;
using Messaging.RabbitMq.Library;
using Messaging.RabbitMq.Library.Configuration;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using System.Reflection;

using Wolverine;

namespace Messaging.Console.App.Configuration;

public static class HostConfigurator
{
    public static IHost BuildKafkaProducerHost()
    {
        var builder = Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                services.AddLogging(loggingBuilder => { services.AddSerilog(loggingBuilder, context.Configuration); });
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
                services.AddLogging(loggingBuilder => { services.AddSerilog(loggingBuilder, context.Configuration); });
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
                services.AddLogging(loggingBuilder => { services.AddSerilog(loggingBuilder, context.Configuration); });
                services.AddHostedService<MessagingProducerServiceHost>();
            });


        builder.UseWolverine(RabbitMqProducerConfigurator.Build);
        var host = builder.Build();
        host.Services.SetupSerilog();
        return host;
    }

    public static IHost BuildRabbitMqDiagnosticsProducerHost()
    {
        var builder = Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                services.AddLogging(loggingBuilder => { services.AddSerilog(loggingBuilder, context.Configuration); });
                services.AddHostedService<MessagingDiagnosticsProducerServiceHost>();
            });

        RabbitMqOptions options = new RabbitMqOptions();
        RabbitMqSetupOptions setupOptions = new RabbitMqSetupOptions();

        var assemblies = new Assembly[]
        {
            typeof(Messaging.RabbitMq.Library.Configuration.Anchor).Assembly,
            typeof(Messaging.Library.Configuration.Anchor).Assembly
        };


        //listening to messages
        var listeningCollection = new ServiceCollection();
        //listeningCollection.AddSingleton<MessageMap<TextMessage>>();

        listeningCollection.AddSingleton<IMessageMap, MessageMap<TextMessage>>((sp) =>
            new MessageMap<TextMessage>()
            {
                QueueName = "text-message-queue",
                DurableQueue = true,//to show customization
            }
        );

        listeningCollection.AddSingleton<IMessageMap, MessageMap<PingMessage>>();
        listeningCollection.AddSingleton<IMessageMap, MessageMap<HeartbeatMessage>>();


        //publishing messages
        var publishingCollection = new ServiceCollection();
        //publishingCollection.AddSingleton<MessageMap<TextMessage>>();
        publishingCollection.AddSingleton<IMessageMap, MessageMap<TextMessage>>((sp) =>
            new MessageMap<TextMessage>()
            {
                QueueName = "text-message-queue",
                DurableQueue = true//to show customization
            }
        );
        publishingCollection.AddSingleton<IMessageMap, MessageMap<PingMessage>>();
        publishingCollection.AddSingleton<IMessageMap, MessageMap<HeartbeatMessage>>();

        TypeToQueueMapper messageQueueNameRegistration = new TypeToQueueMapper();
        messageQueueNameRegistration.Register<PingMessage>("diagnostics-queue");
        messageQueueNameRegistration.Register<HeartbeatMessage>("diagnostics-queue");

        //TODO: use some setup via DI. So the Current services needs build a provider and the provider used to get these objects

        builder.UseWolverine((opt) => RabbitMqdDiagnosticsProducerConfigurator.BuildDiagnostics(opt, options, setupOptions,
            listeningCollection,
            publishingCollection,
            messageQueueNameRegistration,
            assemblies));
        var host = builder.Build();
        host.Services.SetupSerilog();
        return host;
    }

    public static IHost BuildRabbitMqConsumerHost()
    {
        var builder = Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                services.AddLogging(loggingBuilder => { services.AddSerilog(loggingBuilder, context.Configuration); });
                services.AddHostedService<MessagingConsumerServiceHost>();
            });

        builder.UseWolverine(RabbitMqConfigurator.Build);
        var host = builder.Build();
        host.Services.SetupSerilog();
        return host;
    }


    public static IHost BuildRabbitMqDiagnosticsConsumerHost()
    {
        var builder = Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                services.AddLogging(loggingBuilder => { services.AddSerilog(loggingBuilder, context.Configuration); });
                services.AddHostedService<MessagingConsumerServiceHost>();
            });

        builder.UseWolverine(RabbitMqDiagnosticsConfigurator.BuildDiagnostics);
        var host = builder.Build();
        host.Services.SetupSerilog();
        return host;
    }

    public static Task RunHostAsync(IHost host, string title, Microsoft.Extensions.Logging.ILogger logger, CancellationToken cancellationToken)
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
    public static Task RunHostsAsync(IEnumerable<IHost> hosts, string title, Microsoft.Extensions.Logging.ILogger logger, CancellationToken cancellationToken)
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