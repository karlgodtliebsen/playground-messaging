using Messaging.RabbitMq.Library.Configuration;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using System.Reflection;

using Wolverine;
using Wolverine.RabbitMQ;
using Wolverine.RabbitMQ.Internal;

namespace Messaging.Console.App.Configuration;

public static class RabbitMqdDiagnosticsProducerConfigurator
{
    public static void BuildDiagnostics(WolverineOptions opts, RabbitMqOptions options, RabbitMqSetupOptions setupOptions,
        ServiceCollection listeningCollection, ServiceCollection publishingCollection,
        TypeToQueueMapper messageQueueNameRegistration,
        Assembly[]? assemblies = null)
    {
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

        var services = opts.Services;

        services.AddSingleton<IRabbitMqEnvelopeMapper, RabbitMqHeaderEnrich>();
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
                opts.Publish((c) =>
                    {
                        var pr = c.Message(messageMap.MessageType);
                        pr.ToRabbitQueue(queueName);
                    })
                    ;
            }
            else
            {
                opts.Publish((c) =>
                    {
                        var pr = c.Message(messageMap.MessageType);
                        pr.ToRabbitExchange(exchangeName);
                    })
                    ;
            }
        }

        //look up for explicit setting
        //.ToRabbitQueue("textmessage.#")
        //    .ToRabbitQueue(queueName)
        //.ToRabbitExchange(exchangeName)       

        if (assemblies is not null)
        {
            foreach (var assembly in assemblies)
            {
                opts.Discovery.IncludeAssembly(assembly);
            }
        }

    }
}