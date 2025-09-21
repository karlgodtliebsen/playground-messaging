using Messaging.Domain.Library.Messages;
using Messaging.Library.Configuration;
using Messaging.RabbitMq.Library.Configuration;
using Messaging.RabbitMq.Library.LegacySupport;
using Messaging.RabbitMq.WebApi.Controllers;

using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

using System.Reflection;

namespace Messaging.RabbitMq.WebApi.Configuration;

public static class RabbitMqWebApiConfigurator
{

    public static IServiceProvider SetupEventListener(this IServiceProvider serviceProvider)
    {
        var listener = serviceProvider.GetRequiredService<EventHubListener>();
        listener.SetupSubscriptions();
        return serviceProvider;
    }

    public static IServiceCollection AddOptions(this IServiceCollection service, IConfiguration configuration)
    {
        var options = configuration.GetSection(RabbitMqOptions.SectionName).Get<RabbitMqOptions>();
        if (options is null)
        {
            options = new RabbitMqOptions();
        }
        service.TryAddSingleton(Options.Create(options));
        var setupOptions = configuration.GetSection(RabbitMqSetupOptions.SectionName).Get<RabbitMqSetupOptions>();
        if (setupOptions is null)
        {
            setupOptions = new RabbitMqSetupOptions();
        }
        service.TryAddSingleton(Options.Create(setupOptions));
        return service;
    }

    public static IServiceCollection AddProducerServices(this IServiceCollection service, IConfiguration configuration)
    {
        service.AddOptions(configuration);
        var assemblies = new Assembly[]
        {
            typeof(Messaging.RabbitMq.Library.Configuration.Anchor).Assembly,
            typeof(Messaging.RabbitMq.WebApi.Configuration.Anchor).Assembly,
            typeof(Messaging.Domain.Library.Configuration.Anchor).Assembly
        };

        //publishing messages
        var publishingCollection = new ServiceCollection();
        publishingCollection.AddSingleton<IMessageMap, MessageMap>((sp) =>
            new MessageMap<CreateMessage>()
            {
                ExchangeName = "create-message",
                RoutingKey = "create-message-route",
                BindingPattern = "create-message.#",
                QueueName = "create-message-queue",
            }
        );
        service.TryAddKeyedSingleton<Assembly[]>("producer", assemblies);
        service.TryAddKeyedSingleton<TypeToQueueMapper>("producer");
        service.TryAddKeyedSingleton<IServiceCollection>("producer", publishingCollection);
        return service;
    }
    public static IServiceCollection AddLegacyProducerServices(this IServiceCollection service, IConfiguration configuration)
    {
        service.AddLegacyRabbitMqServices(configuration);
        service.AddEventHubServices(configuration);
        var assemblies = new Assembly[]
        {
            typeof(Messaging.RabbitMq.Library.Configuration.Anchor).Assembly,
            typeof(Messaging.RabbitMq.WebApi.Configuration.Anchor).Assembly,
            typeof(Messaging.Domain.Library.Configuration.Anchor).Assembly
        };


        //publishing messages
        var publishingCollection = new ServiceCollection();
        publishingCollection.AddSingleton<IMessageMap, MessageMap>((sp) =>
            new MessageMap<CreateMessage>()
            {
                ExchangeName = "create-message",
                RoutingKey = "create-message-route",
                BindingPattern = "create-message.#",
                QueueName = "create-message-queue",
            }
        );
        LegacyTypeMapper mapper = new LegacyTypeMapper();
        ////mapper.Register<TextMessage>(typeof(TextMessage).FullName!);
        ////mapper.Register<PingMessage>(typeof(PingMessage).FullName!);
        ////mapper.Register<HeartbeatMessage>(typeof(HeartbeatMessage).FullName!);

        service.TryAddSingleton<LegacyTypeMapper>(mapper);

        var messageQueueNameRegistration = new TypeToQueueMapper();
        //messageQueueNameRegistration.Register<PingMessage>("diagnostics-queue");
        //messageQueueNameRegistration.Register<HeartbeatMessage>("diagnostics-queue");
        service.TryAddKeyedSingleton<TypeToQueueMapper>("producer", messageQueueNameRegistration);
        service.TryAddKeyedSingleton<Assembly[]>("producer", assemblies);
        service.TryAddKeyedSingleton<IServiceCollection>("producer", publishingCollection);
        return service;
    }

    public static IServiceCollection AddConsumerServices(this IServiceCollection service, IConfiguration configuration)
    {
        service.AddOptions(configuration);

        // service.AddCustomizedRabbitMqServices(configuration);
        service.AddEventHubServices(configuration);
        var assemblies = new Assembly[]
        {
            typeof(Messaging.RabbitMq.Library.Configuration.Anchor).Assembly,
            typeof(Messaging.RabbitMq.WebApi.Configuration.Anchor).Assembly,
            typeof(Messaging.Domain.Library.Configuration.Anchor).Assembly
        };


        //listening to messages
        var listeningCollection = new ServiceCollection();
        //listeningCollection.AddSingleton<MessageMap<TextMessage>>();
        listeningCollection.AddSingleton<IMessageMap, MessageMap>((sp) =>
            new MessageMap<CreateMessage>()
            {
                ExchangeName = "create-message",
                RoutingKey = "create-message-route",
                BindingPattern = "create-message.#",
                QueueName = "create-message-queue",
            }
        );
        //listeningCollection.AddSingleton<IMessageMap, MessageMap<TextMessage>>((sp) =>
        //    new MessageMap<TextMessage>()
        //    {
        //        QueueName = "text-message-queue",
        //        DurableQueue = true,//to show customization
        //    }
        //);

        //listeningCollection.AddSingleton<IMessageMap, MessageMap<PingMessage>>();
        //listeningCollection.AddSingleton<IMessageMap, MessageMap<HeartbeatMessage>>();

        // var messageQueueNameRegistration = new TypeToQueueMapper();
        //messageQueueNameRegistration.Register<PingMessage>("diagnostics-queue");
        //messageQueueNameRegistration.Register<HeartbeatMessage>("diagnostics-queue");

        service.TryAddKeyedSingleton<TypeToQueueMapper>("consumer");
        service.TryAddKeyedSingleton<Assembly[]>("consumer", assemblies);
        service.TryAddKeyedSingleton<IServiceCollection>("consumer", listeningCollection);
        return service;
    }

    public static IServiceCollection AddLegacyConsumerServices(this IServiceCollection service, IConfiguration configuration)
    {
        service.AddLegacyRabbitMqServices(configuration);
        service.AddEventHubServices(configuration);

        var assemblies = new Assembly[]
        {
            typeof(Messaging.RabbitMq.Library.Configuration.Anchor).Assembly,
            typeof(Messaging.RabbitMq.WebApi.Configuration.Anchor).Assembly,
            typeof(Messaging.Domain.Library.Configuration.Anchor).Assembly
        };


        //listening to messages
        var listeningCollection = new ServiceCollection();
        listeningCollection.AddSingleton<IMessageMap, MessageMap>((sp) =>
            new MessageMap<CreateMessage>()
            {
                ExchangeName = "create-message",
                RoutingKey = "create-message-route",
                BindingPattern = "create-message.#",
                QueueName = "create-message-queue",
            }
        );

        LegacyTypeMapper mapper = new LegacyTypeMapper();
        //mapper.Register<TextMessage>(typeof(TextMessage).FullName!);
        //mapper.Register<PingMessage>(typeof(PingMessage).FullName!);
        //mapper.Register<HeartbeatMessage>(typeof(HeartbeatMessage).FullName!);

        service.TryAddSingleton<LegacyTypeMapper>(mapper);

        var messageQueueNameRegistration = new TypeToQueueMapper();
        //messageQueueNameRegistration.Register<PingMessage>("diagnostics-queue");
        //messageQueueNameRegistration.Register<HeartbeatMessage>("diagnostics-queue");
        service.TryAddKeyedSingleton<TypeToQueueMapper>("consumer", messageQueueNameRegistration);

        service.TryAddKeyedSingleton<Assembly[]>("consumer", assemblies);
        service.TryAddKeyedSingleton<IServiceCollection>("consumer", listeningCollection);
        return service;
    }

}