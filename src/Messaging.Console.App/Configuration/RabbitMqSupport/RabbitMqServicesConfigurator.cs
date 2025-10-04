using Messaging.Domain.Library.DemoMessages;
using Messaging.EventHub.Library.Configuration;
using Messaging.RabbitMq.Library.Configuration;
using Messaging.RabbitMq.Library.LegacySupport;
using Messaging.RabbitMq.Library.MessageSupport;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using System.Reflection;

namespace Messaging.Console.App.Configuration.RabbitMqSupport;

public static class RabbitMqServicesConfigurator
{

    //At the moment this is geared towards rabbitmq message registration, but use the message part as general registration configuration, then it can be used from other platforms as well
    public static IServiceCollection AddProducerServices(this IServiceCollection service, IConfiguration configuration)
    {
        service.AddLegacyRabbitMqServices(configuration);

        var assemblies = new Assembly[]
        {
            typeof(Domain.Library.Configuration.Anchor).Assembly
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

        service.TryAddKeyedSingleton("producer", messageQueueNameRegistration);

        LegacyTypeMapper mapper = new LegacyTypeMapper();
        mapper.Register<TextMessage>(typeof(TextMessage).FullName!);
        mapper.Register<PingMessage>(typeof(PingMessage).FullName!);
        mapper.Register<HeartbeatMessage>(typeof(HeartbeatMessage).FullName!);

        service.TryAddSingleton(mapper);


        service.TryAddKeyedSingleton("producer", assemblies);
        service.TryAddKeyedSingleton<IServiceCollection>("producer", publishingCollection);
        return service;
    }
    public static IServiceCollection AddConsumerServices(this IServiceCollection service, IConfiguration configuration)
    {
        service.AddLegacyRabbitMqServices(configuration);
        service.AddEventHubServices(configuration);

        var assemblies = new Assembly[]
        {
            typeof(Domain.Library.Configuration.Anchor).Assembly
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

        service.TryAddSingleton(mapper);
        service.TryAddKeyedSingleton("consumer", messageQueueNameRegistration);
        service.TryAddKeyedSingleton("consumer", assemblies);
        service.TryAddKeyedSingleton<IServiceCollection>("consumer", listeningCollection);
        return service;
    }
}