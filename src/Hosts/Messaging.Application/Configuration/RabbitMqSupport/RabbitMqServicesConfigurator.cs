using Messaging.Domain.Library.DemoMessages;
using Messaging.EventHub.Library.Configuration;
using Messaging.RabbitMq.Library.Configuration;
using Messaging.RabbitMq.Library.LegacySupport;
using Messaging.RabbitMq.Library.MessageSupport;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using System.Reflection;

namespace Messaging.Application.Configuration.RabbitMqSupport;

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
        var registry = new MessageMetaDataRegistry();
        registry.Register<TextMessage>("text-message", "text-message.route", "text-message.#", "text-message-queue");//setting queue name is optional
        registry.Register<PingMessage>("diagnostics", "diagnostics.ping", "diagnostics.ping.#", "diagnostics-queue");//setting queue name is optional
        registry.Register<HeartbeatMessage>("diagnostics", "diagnostics.heartbeat", "diagnostics.heartbeat.#", "diagnostics-queue");//setting queue name is optional
        publishingCollection.TryAddSingleton(registry);

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

        ////listening to messages
        var listeningCollection = new ServiceCollection();

        var registry = new MessageMetaDataRegistry();
        registry.Register<TextMessage>("text-message", "text-message.route", "text-message.#", "text-message-queue");//setting queue name is optional
        registry.Register<PingMessage>("diagnostics", "diagnostics.ping", "diagnostics.ping.#", "diagnostics-queue");//setting queue name is optional
        registry.Register<HeartbeatMessage>("diagnostics", "diagnostics.heartbeat", "diagnostics.heartbeat.#", "diagnostics-queue");//setting queue name is optional
        listeningCollection.TryAddSingleton(registry);

        LegacyTypeMapper mapper = new LegacyTypeMapper();
        mapper.Register<TextMessage>(typeof(TextMessage).FullName!);
        mapper.Register<PingMessage>(typeof(PingMessage).FullName!);
        mapper.Register<HeartbeatMessage>(typeof(HeartbeatMessage).FullName!);

        service.TryAddSingleton(mapper);
        service.TryAddKeyedSingleton("consumer", assemblies);
        service.TryAddKeyedSingleton<IServiceCollection>("consumer", listeningCollection);
        return service;
    }
}