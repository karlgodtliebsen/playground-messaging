using Messaging.Console.App.Services;
using Messaging.Kafka.Library.Configuration;
using Messaging.Library.Configuration;
using Messaging.RabbitMq.Library;
using Messaging.RabbitMq.Library.Configuration;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
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

    public static IServiceCollection AddProducerServices(this IServiceCollection service, IConfiguration configuration)
    {
        service.AddRabbitMqServices(configuration);

        var assemblies = new Assembly[]
        {
            typeof(Messaging.RabbitMq.Library.Configuration.Anchor).Assembly,
            typeof(Messaging.Domain.Library.Configuration.Anchor).Assembly
        };


        //publishing messages
        var publishingCollection = new ServiceCollection();
        publishingCollection.AddSingleton<IMessageMap, MessageMap<TextMessage>>((sp) =>
            new MessageMap<TextMessage>()
            {
                QueueName = "text-message-queue",//to show customization
            }
        );
        publishingCollection.AddSingleton<IMessageMap, MessageMap<PingMessage>>();
        publishingCollection.AddSingleton<IMessageMap, MessageMap<HeartbeatMessage>>();

        var messageQueueNameRegistration = new TypeToQueueMapper();
        messageQueueNameRegistration.Register<PingMessage>("diagnostics-queue");
        messageQueueNameRegistration.Register<HeartbeatMessage>("diagnostics-queue");

        service.TryAddKeyedSingleton<TypeToQueueMapper>("producer", messageQueueNameRegistration);

        LegacyTypeMapper mapper = new LegacyTypeMapper();
        mapper.Register<TextMessage>(typeof(TextMessage).FullName!);
        mapper.Register<PingMessage>(typeof(PingMessage).FullName!);
        mapper.Register<HeartbeatMessage>(typeof(HeartbeatMessage).FullName!);

        service.TryAddSingleton<LegacyTypeMapper>(mapper);


        service.TryAddKeyedSingleton<Assembly[]>("producer", assemblies);
        service.TryAddKeyedSingleton<IServiceCollection>("producer", publishingCollection);
        return service;
    }
    public static IHost BuildRabbitMqProducerHost()
    {
        var builder = Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                services.AddProducerServices(context.Configuration);
                services.AddLogging(loggingBuilder => { services.AddSerilog(loggingBuilder, context.Configuration); });
                services.AddHostedService<MessagingDiagnosticsProducerServiceHost>();
            });

        builder.UseWolverine((opt) => RabbitMqConfigurator.BuildWolverine(opt));
        var host = builder.Build();
        host.Services.SetupSerilog();
        return host;
    }

    public static IServiceCollection AddConsumerServices(this IServiceCollection service, IConfiguration configuration)
    {
        service.AddRabbitMqServices(configuration);
        service.AddLibraryServices(configuration);

        var assemblies = new Assembly[]
        {
            typeof(Messaging.RabbitMq.Library.Configuration.Anchor).Assembly,
            typeof(Messaging.Domain.Library.Configuration.Anchor).Assembly
        };

        //listening to messages
        var listeningCollection = new ServiceCollection();
        listeningCollection.AddSingleton<IMessageMap, MessageMap<TextMessage>>((sp) =>
            new MessageMap<TextMessage>()
            {
                QueueName = "text-message-queue",//to show customization
            }
        );

        listeningCollection.AddSingleton<IMessageMap, MessageMap<PingMessage>>();
        listeningCollection.AddSingleton<IMessageMap, MessageMap<HeartbeatMessage>>();

        var messageQueueNameRegistration = new TypeToQueueMapper();
        messageQueueNameRegistration.Register<PingMessage>("diagnostics-queue");
        messageQueueNameRegistration.Register<HeartbeatMessage>("diagnostics-queue");

        LegacyTypeMapper mapper = new LegacyTypeMapper();
        mapper.Register<TextMessage>(typeof(TextMessage).FullName!);
        mapper.Register<PingMessage>(typeof(PingMessage).FullName!);
        mapper.Register<HeartbeatMessage>(typeof(HeartbeatMessage).FullName!);

        service.TryAddSingleton<LegacyTypeMapper>(mapper);
        service.TryAddKeyedSingleton<TypeToQueueMapper>("consumer", messageQueueNameRegistration);
        service.TryAddKeyedSingleton<Assembly[]>("consumer", assemblies);
        service.TryAddKeyedSingleton<IServiceCollection>("consumer", listeningCollection);
        return service;
    }

    public static IHost BuildRabbitMqConsumerHost()
    {
        var builder = Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                services
                    .AddConsumerServices(context.Configuration)
                    .AddLibraryServices(context.Configuration)
                    .AddApplicationServices(context.Configuration)
                    ;
                services.AddLogging(loggingBuilder => { services.AddSerilog(loggingBuilder, context.Configuration); });
                services.AddHostedService<MessagingConsumerServiceHost>();
            });
        builder.UseWolverine((opt) => RabbitMqConfigurator.BuildWolverine(opt));
        var host = builder.Build();
        host.Services.SetupChannelListener();
        host.Services.SetupSerilog();
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
                    .AddLibraryServices(context.Configuration)
                    .AddApplicationServices(context.Configuration)
                    ;
                services.AddLogging(loggingBuilder => { services.AddSerilog(loggingBuilder, context.Configuration); });
                services.AddHostedService<MessagingDiagnosticsProducerServiceHost>();
            });
        builder.UseWolverine((opt) => RabbitMqConfigurator.BuildWolverine(opt));
        var host = builder.Build();
        host.Services.SetupChannelListener();
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