using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using System.Reflection;

using Wolverine;
using Wolverine.RabbitMQ;
using Wolverine.RabbitMQ.Internal;

namespace Messaging.RabbitMq.Library.Configuration;

public static class RabbitMqDiagnosticsConfigurator
{

    public static IServiceCollection AddServices(this IServiceCollection service, IConfiguration configuration)
    {
        var options = configuration.GetSection(RabbitMqOptions.SectionName).Get<RabbitMqOptions>();
        var setupOptions = configuration.GetSection(RabbitMqSetupOptions.SectionName).Get<RabbitMqSetupOptions>();
        if (options is null)
        {
            options = new RabbitMqOptions();
        }
        if (setupOptions is null)
        {
            setupOptions = new RabbitMqSetupOptions();
        }
        service.TryAddSingleton(Options.Create(options));
        service.TryAddSingleton(Options.Create(setupOptions));
        service.TryAddSingleton<IRabbitMqEnvelopeMapper, RabbitMqHeaderEnrich>();
        service.TryAddSingleton<LegacyTypeMapper>();
        service.TryAddSingleton<TypeToQueueMapper>();

        return service;
    }

    public static void BuildWolverine(WolverineOptions opts,
                        RabbitMqOptions options,
                        RabbitMqSetupOptions setupOptions,
                        ServiceCollection listeningCollection,
                        ServiceCollection publishingCollection,
                        TypeToQueueMapper messageQueueNameRegistration,
                        Action<WolverineOptions>? extendAction = null,
                        Assembly[]? assemblies = null)
    {
        //TODO: build services and lookup LegacyTypeMapper and the other types



        // Basic RabbitMQ connection
        var rabbit = opts.UseRabbitMq(rabbit =>
        {
            rabbit.HostName = options.HostName;
            rabbit.Port = options.Port;
            rabbit.UserName = options.UserName;
            rabbit.Password = options.Password;
            rabbit.VirtualHost = options.VirtualHost;
            rabbit.RequestedHeartbeat = options.Heartbeat;
            rabbit.RequestedConnectionTimeout = options.DefaultConnectionTimeout;
        });

        rabbit
            .AutoProvision()
            .AutoPurgeOnStartup()
            ;
        extendAction?.Invoke(opts);
        var services = opts.Services;
        if (setupOptions.UseLegacyMapping)
        {
            rabbit
                .ConfigureSenders(s => s.UseInterop(new RabbitMqHeaderEnrich())) // all publishers
                .ConfigureListeners(l => l.UseInterop(new RabbitMqHeaderEnrich())); // all consumers
        }
        if (setupOptions.UseDebugLogging)
        {
            // Enable detailed logging
            services.AddLogging(logging =>
            {
                logging.SetMinimumLevel(LogLevel.Debug);
                logging.AddConsole();
            });
        }

        var listeningMessagesMaps = listeningCollection.BuildServiceProvider().GetServices<IMessageMap>().ToList();

        if (setupOptions.DeclareExchanges)
        {
            var exchanges = listeningMessagesMaps.GroupBy(m => m.Exchange).Select(group => group.First()).ToList();
            foreach (var messageMap in exchanges)
            {
                ArgumentNullException.ThrowIfNull(messageMap);
                var exchangeName = messageMap.Exchange;
                rabbit.DeclareExchange(exchangeName, exchange =>
                {
                    exchange.ExchangeType = messageMap.ExchangeType;
                    exchange.IsDurable = messageMap.DurableExchange;
                    exchange.AutoDelete = messageMap.AutoDeleteExchange;
                });
            }
        }

        //setup the listening
        foreach (var messageMap in listeningMessagesMaps)
        {
            ArgumentNullException.ThrowIfNull(messageMap);
            var exchangeName = messageMap.Exchange;
            var queueName = messageMap.QueueName;
            if (messageMap.UseQueue)
            {
                //look up for explicit setting
                if (queueName is null)
                {
                    queueName = messageQueueNameRegistration.TryLookup(messageMap.MessageType);
                    if (queueName is null)
                    {
                        queueName = "default-queue";
                    }
                }
                if (!string.IsNullOrEmpty(queueName))
                {
                    opts.ListenToRabbitQueue(queueName, queue =>
                    {
                        //Can we not bind to exchange??
                        queue.BindExchange(exchangeName, messageMap.BindingPattern);
                        queue.IsDurable = messageMap.DurableQueue;
                        queue.AutoDelete = messageMap.AutoDeleteQueue;
                        queue.PurgeOnStartup = messageMap.PurgeOnStartup;
                        queue.IsExclusive = messageMap.Exclusive;
                        queue.QueueType = messageMap.QueueType;
                        //queue.DeadLetterQueue = new DeadLetterQueue()
                        //queue.TimeToLive(TimeSpan.FromSeconds(messageMap.TimeToLive));
                    });
                }
            }//TODO.. other options?
        }


        var publishingMessagesMaps = publishingCollection.BuildServiceProvider().GetServices<IMessageMap>().ToList();

        if (setupOptions.DeclareExchanges)
        {
            var exchanges = publishingMessagesMaps.GroupBy(m => m.Exchange).Select(group => group.First()).ToList();
            foreach (var messageMap in exchanges)
            {
                ArgumentNullException.ThrowIfNull(messageMap);
                var exchangeName = messageMap.Exchange;
                rabbit.DeclareExchange(exchangeName, exchange =>
                {
                    exchange.ExchangeType = messageMap.ExchangeType;
                    exchange.IsDurable = messageMap.DurableExchange;
                    exchange.AutoDelete = messageMap.AutoDeleteExchange;
                });
            }
        }
        foreach (var messageMap in publishingMessagesMaps)
        {
            ArgumentNullException.ThrowIfNull(messageMap);
            var exchangeName = messageMap.Exchange;
            var queueName = messageMap.QueueName;
            if (messageMap.UseQueue)
            {
                if (queueName is null)
                {
                    queueName = messageQueueNameRegistration.TryLookup(messageMap.MessageType);
                    if (queueName is null)
                    {
                        queueName = "default-queue";
                    }
                }
                if (!string.IsNullOrEmpty(queueName))
                {
                    opts.Publish((c) =>
                    {
                        var pr = c.Message(messageMap.MessageType);
                        pr.ToRabbitQueue(queueName);
                    });
                }
            }
            else
            if (messageMap.UseExchange)
            {
                opts.Publish((c) =>
                {
                    var pr = c.Message(messageMap.MessageType);
                    pr.ToRabbitExchange(exchangeName);
                });
            }
            else
            {
                opts.Publish((c) =>
                {
                    var pr = c.Message(messageMap.MessageType);
                    pr.ToRabbitQueue(messageMap.BindingPattern);
                });
            }
        }

        if (assemblies is not null)
        {
            foreach (var assembly in assemblies)
            {
                opts.Discovery.IncludeAssembly(assembly);
            }
        }

    }
}