using Messaging.Domain.Library.DemoMessages;
using Messaging.Domain.Library.Orders;
using Messaging.Domain.Library.Payments;
using Messaging.Domain.Library.SimpleMessages;
using Messaging.EventHub.Library.Configuration;
using Messaging.RabbitMq.Library.Configuration;
using Messaging.RabbitMq.Library.LegacySupport;
using Messaging.RabbitMq.Library.MessageSupport;
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
            typeof(Messaging.RabbitMq.WebApi.Configuration.Anchor).Assembly,
            typeof(Messaging.Domain.Library.Configuration.Anchor).Assembly
        };

        //publishing messages
        var publishingCollection = new ServiceCollection();
        var registry = new MessageMetaDataRegistry();
        registry.Register<TextMessage>("text-message", "text-message.route", "text-message.#");
        registry.Register<PingMessage>("diagnostics", "diagnostics.ping", "diagnostics.ping.#");
        registry.Register<HeartbeatMessage>("diagnostics", "diagnostics.heartbeat", "diagnostics.heartbeat.#");

        registry.Register<InformationMessage>("message", "message", "message.#");
        registry.Register<CreateMessage>("message", "message", "message.#");

        registry.Register<PaymentProcessed>("payments", "payments", "payments.#");

        registry.Register<OrderUpdated>("orders", "orders.updated", "orders.updated.#");
        registry.Register<OrderCreated>("orders", "orders.created", "orders.created.#");


        publishingCollection.TryAddSingleton(registry);

        service.TryAddKeyedSingleton<Assembly[]>("producer", assemblies);
        service.TryAddKeyedSingleton<IServiceCollection>("producer", publishingCollection);
        return service;
    }
    public static IServiceCollection AddLegacyProducerServices(this IServiceCollection service, IConfiguration configuration)
    {
        service.AddLegacyRabbitMqServices(configuration);
        service.AddEventHubServices(configuration);
        var assemblies = new Assembly[]
        {
            typeof(Messaging.RabbitMq.WebApi.Configuration.Anchor).Assembly,
            typeof(Messaging.Domain.Library.Configuration.Anchor).Assembly
        };


        //publishing messages
        var publishingCollection = new ServiceCollection();
        var registry = new MessageMetaDataRegistry();
        registry.Register<TextMessage>("text-message", "text-message.route", "text-message.#");
        registry.Register<PingMessage>("diagnostics", "diagnostics.ping", "diagnostics.ping.#");
        registry.Register<HeartbeatMessage>("diagnostics", "diagnostics.heartbeat", "diagnostics.heartbeat.#");

        registry.Register<InformationMessage>("message", "message", "message.#");
        registry.Register<CreateMessage>("message", "message", "message.#");

        registry.Register<PaymentProcessed>("payments", "payments", "payments.#");

        registry.Register<OrderUpdated>("orders", "orders.updated", "orders.updated.#");
        registry.Register<OrderCreated>("orders", "orders.created", "orders.created.#");

        publishingCollection.TryAddSingleton(registry);

        LegacyTypeMapper mapper = new LegacyTypeMapper();
        ////mapper.Register<TextMessage>(typeof(TextMessage).FullName!);
        ////mapper.Register<PingMessage>(typeof(PingMessage).FullName!);
        ////mapper.Register<HeartbeatMessage>(typeof(HeartbeatMessage).FullName!);

        service.TryAddSingleton<LegacyTypeMapper>(mapper);

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
            typeof(Messaging.RabbitMq.WebApi.Configuration.Anchor).Assembly,
            typeof(Messaging.Domain.Library.Configuration.Anchor).Assembly
        };


        //listening to messages
        var listeningCollection = new ServiceCollection();

        MessageMetaDataRegistry registry = new MessageMetaDataRegistry();
        registry.Register<TextMessage>("text-message", "text-message.route", "text-message.#");
        registry.Register<PingMessage>("diagnostics", "diagnostics.ping", "diagnostics.ping.#");
        registry.Register<HeartbeatMessage>("diagnostics", "diagnostics.heartbeat", "diagnostics.heartbeat.#");
        service.TryAddKeyedSingleton<IServiceCollection>("consumer", listeningCollection);
        service.TryAddKeyedSingleton<Assembly[]>("consumer", assemblies);
        return service;
    }

    public static IServiceCollection AddLegacyConsumerServices(this IServiceCollection service, IConfiguration configuration)
    {
        service.AddLegacyRabbitMqServices(configuration);
        service.AddEventHubServices(configuration);

        var assemblies = new Assembly[]
        {
            typeof(Messaging.RabbitMq.WebApi.Configuration.Anchor).Assembly,
            typeof(Messaging.Domain.Library.Configuration.Anchor).Assembly
        };


        //listening to messages
        var listeningCollection = new ServiceCollection();
        MessageMetaDataRegistry registry = new MessageMetaDataRegistry();
        registry.Register<TextMessage>("text-message", "text-message.route", "text-message.#");
        registry.Register<PingMessage>("diagnostics", "diagnostics.ping", "diagnostics.ping.#");
        registry.Register<HeartbeatMessage>("diagnostics", "diagnostics.heartbeat", "diagnostics.heartbeat.#");
        service.TryAddKeyedSingleton<IServiceCollection>("consumer", listeningCollection);

        LegacyTypeMapper mapper = new LegacyTypeMapper();
        //mapper.Register<TextMessage>(typeof(TextMessage).FullName!);
        //mapper.Register<PingMessage>(typeof(PingMessage).FullName!);
        //mapper.Register<HeartbeatMessage>(typeof(HeartbeatMessage).FullName!);

        service.TryAddSingleton<LegacyTypeMapper>(mapper);


        service.TryAddKeyedSingleton<Assembly[]>("consumer", assemblies);
        service.TryAddKeyedSingleton<IServiceCollection>("consumer", listeningCollection);
        return service;
    }

}