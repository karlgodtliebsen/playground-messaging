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

public static class RabbitMqConfigurator
{
    private const string Consumer = "consumer";
    private const string Producer = "producer";

    public static IServiceCollection AddRabbitMqServices(this IServiceCollection service, IConfiguration configuration)
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
        service.TryAddSingleton<TypeToQueueMapper>();

        return service;
    }

    public static void BuildWolverine(WolverineOptions opts, IServiceCollection serviceCollection, Action<WolverineOptions>? extendAction = null)
    {
        var sp = serviceCollection.BuildServiceProvider();
        var listeningCollection = sp.GetKeyedService<IServiceCollection>(Consumer);
        var publishingCollection = sp.GetKeyedService<IServiceCollection>(Producer);
        var options = sp.GetRequiredService<IOptions<RabbitMqOptions>>().Value;
        var setupOptions = sp.GetRequiredService<IOptions<RabbitMqSetupOptions>>().Value;

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

        rabbit.AutoProvision();
        if (setupOptions.AutoPurge) rabbit.AutoPurgeOnStartup();

        extendAction?.Invoke(opts);
        var services = opts.Services;
        if (setupOptions.UseLegacyMapping)
        {
            var enrich = sp.GetRequiredService<IRabbitMqEnvelopeMapper>();
            rabbit
                .ConfigureSenders(s => s.UseInterop(enrich)) // all publishers
                .ConfigureListeners(l => l.UseInterop(enrich)); // all consumers
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
        if (listeningCollection is not null)
        {
            var messageQueueNameRegistration = sp.GetRequiredKeyedService<TypeToQueueMapper>(Consumer);
            var assemblies = sp.GetKeyedService<Assembly[]>(Consumer);

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

            //wiring up the listening
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
                } //TODO: should we handle other options or is this enough to fulfill the simple goal
            }
            if (assemblies is not null)
            {
                foreach (var assembly in assemblies)
                {
                    opts.Discovery.IncludeAssembly(assembly);
                }
            }
        }
        if (publishingCollection is not null)
        {
            var assemblies = sp.GetKeyedService<Assembly[]>(Producer);

            var messageQueueNameRegistration = sp.GetRequiredKeyedService<TypeToQueueMapper>(Producer);

            var publishingMessagesMaps =
                publishingCollection.BuildServiceProvider().GetServices<IMessageMap>().ToList();

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
            } //TODO: should we handle other options or is this enough to fulfill the simple goal
            if (assemblies is not null)
            {
                foreach (var assembly in assemblies)
                {
                    opts.Discovery.IncludeAssembly(assembly);
                }
            }
        }



    }
}