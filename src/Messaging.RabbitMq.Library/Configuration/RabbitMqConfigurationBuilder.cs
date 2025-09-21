using Messaging.RabbitMq.Library.LegacySupport;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using System.Reflection;

using Wolverine;
using Wolverine.RabbitMQ;
using Wolverine.RabbitMQ.Internal;

namespace Messaging.RabbitMq.Library.Configuration;

public static class RabbitMqConfigurationBuilder
{
    private const string Consumer = "consumer";
    private const string Producer = "producer";
    private const string Monitor = "monitor";

    public static void BuildRabbitMqSetupUsingWolverine(WolverineOptions opts, Action<WolverineOptions>? extendAction = null)
    {
        var services = opts.Services;
        var sp = services.BuildServiceProvider();
        var listeningCollection = sp.GetKeyedService<IServiceCollection>(Consumer);
        var publishingCollection = sp.GetKeyedService<IServiceCollection>(Producer);
        var options = sp.GetRequiredService<IOptions<RabbitMqOptions>>().Value;
        var setupOptions = sp.GetRequiredService<IOptions<RabbitMqSetupOptions>>().Value;
        var queues = new List<string>();

        // Setup RabbitMQ connection
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

        rabbit.AutoProvision();
        extendAction?.Invoke(opts);

        if (setupOptions.UseLegacyMapping)
        {
            var enrich = sp.GetService<IRabbitMqEnvelopeMapper>();
            if (enrich is not null)
            {
                rabbit
                    .ConfigureSenders(s => s.UseInterop(enrich)) // all publishers
                    .ConfigureListeners(l => l.UseInterop(enrich)); // all consumers
            }
        }

        if (setupOptions.AutoPurge)
        {
            rabbit.AutoPurgeOnStartup();
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
        var messageQueueNameRegistration = sp.GetKeyedService<TypeToQueueMapper>(Consumer);
        if (listeningCollection is not null && messageQueueNameRegistration is not null)
        {

            var listeningMessagesMaps = listeningCollection.BuildServiceProvider().GetServices<IMessageMap>().ToList();

            if (setupOptions.DeclareExchanges)
            {
                var exchanges = listeningMessagesMaps.GroupBy(m => m.ExchangeName).Select(group => group.First()).ToList();
                foreach (var messageMap in exchanges)
                {
                    ArgumentNullException.ThrowIfNull(messageMap);
                    var exchangeName = messageMap.ExchangeName;
                    rabbit.DeclareExchange(exchangeName, exchange =>
                    {
                        exchange.ExchangeType = messageMap.ExchangeType;
                        exchange.IsDurable = messageMap.DurableExchange;
                        exchange.AutoDelete = messageMap.AutoDeleteExchange;
                    });
                }
            }

            //wiring up the listening
            foreach (var messageMap in listeningMessagesMaps)
            {
                ArgumentNullException.ThrowIfNull(messageMap);
                var exchangeName = messageMap.ExchangeName;
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
                        queues.Add(queueName);
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
                } //TODO: should we handle other options or is this enough to fulfill the simple goal
            }
            var assemblies = sp.GetKeyedService<Assembly[]>(Consumer);
            if (assemblies is not null)
            {
                foreach (var assembly in assemblies)
                {
                    opts.Discovery.IncludeAssembly(assembly);
                }
            }
        }
        messageQueueNameRegistration = sp.GetKeyedService<TypeToQueueMapper>(Producer);

        if (publishingCollection is not null && messageQueueNameRegistration is not null)
        {
            var publishingMessagesMaps =
                publishingCollection.BuildServiceProvider().GetServices<IMessageMap>().ToList();

            if (setupOptions.DeclareExchanges)
            {
                var exchanges = publishingMessagesMaps.GroupBy(m => m.ExchangeName).Select(group => group.First()).ToList();
                foreach (var messageMap in exchanges)
                {
                    ArgumentNullException.ThrowIfNull(messageMap);
                    var exchangeName = messageMap.ExchangeName;
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
                var exchangeName = messageMap.ExchangeName;
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
                        queues.Add(queueName);
                        opts.Publish((c) =>
                        {
                            var pr = c.Message(messageMap.MessageType);
                            pr.ToRabbitQueue(queueName);
                        });
                    }
                }
                else if (messageMap.UseExchange)
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
            } //TODO: should we handle other options or is this enough to fulfill the simple goal that we have

            var assemblies = sp.GetKeyedService<Assembly[]>(Producer);
            if (assemblies is not null)
            {
                foreach (var assembly in assemblies)
                {
                    opts.Discovery.IncludeAssembly(assembly);
                }
            }
        }
        services.AddKeyedSingleton(Monitor, Options.Create(queues.Distinct().ToArray()));
    }
}