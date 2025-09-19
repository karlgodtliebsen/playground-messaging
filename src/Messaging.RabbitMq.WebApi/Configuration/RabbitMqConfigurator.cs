using Messaging.RabbitMq.Library;
using Messaging.RabbitMq.Library.Configuration;

using Microsoft.Extensions.DependencyInjection.Extensions;

using System.Reflection;

namespace Messaging.RabbitMq.WebApi.Configuration;

public static class RabbitMqConfigurator
{

    public static IServiceCollection AddProducerServices(this IServiceCollection service, IConfiguration configuration)
    {
        service.AddRabbitMqServices(configuration);


        var assemblies = new Assembly[]
        {
            typeof(Messaging.RabbitMq.Library.Configuration.Anchor).Assembly,
            typeof(Messaging.Library.Configuration.Anchor).Assembly
        };


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

        var messageQueueNameRegistration = new TypeToQueueMapper();
        messageQueueNameRegistration.Register<PingMessage>("diagnostics-queue");
        messageQueueNameRegistration.Register<HeartbeatMessage>("diagnostics-queue");

        service.TryAddKeyedSingleton<Assembly[]>("producer", assemblies);
        service.TryAddKeyedSingleton<IServiceCollection>("producer", publishingCollection);
        return service;
    }

    public static IServiceCollection AddConsumerServices(this IServiceCollection service, IConfiguration configuration)
    {
        service.AddRabbitMqServices(configuration);

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

        var messageQueueNameRegistration = new TypeToQueueMapper();
        messageQueueNameRegistration.Register<PingMessage>("diagnostics-queue");
        messageQueueNameRegistration.Register<HeartbeatMessage>("diagnostics-queue");

        service.TryAddKeyedSingleton<Assembly[]>("consumer", assemblies);
        service.TryAddKeyedSingleton<IServiceCollection>("consumer", listeningCollection);
        return service;
    }

}