using Messaging.Library.Configuration;
using Messaging.RabbitMq.Library;
using Messaging.RabbitMq.Library.Configuration;
using Messaging.RabbitMq.Library.LegacySupport;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using System.Reflection;

namespace Messaging.Console.App.Configuration;

public static class HostingServicesConfigurator
{
    public static IServiceCollection AddProducerServices(this IServiceCollection service, IConfiguration configuration)
    {
        service.AddLegacyRabbitMqServices(configuration);

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
    public static IServiceCollection AddConsumerServices(this IServiceCollection service, IConfiguration configuration)
    {
        service.AddLegacyRabbitMqServices(configuration);
        service.AddEventHubServices(configuration);

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
}