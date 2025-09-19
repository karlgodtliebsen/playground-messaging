using Messaging.RabbitMq.Library;
using Messaging.RabbitMq.Library.Configuration;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using RabbitMQ.Client;

using Wolverine;
using Wolverine.RabbitMQ;
using Wolverine.RabbitMQ.Internal;

using ExchangeType = Wolverine.RabbitMQ.ExchangeType;
using IMessage = Messaging.RabbitMq.Library.IMessage;

namespace Messaging.Console.App.Configuration;

public static class RabbitMqdDiagnosticsProducerConfigurator
{
    public static void BuildDiagnostics(WolverineOptions opts, RabbitMqOptions options, RabbitMqSetupOptions setupOptions)
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
            .AutoPurgeOnStartup();
        var services = opts.Services;

        services.AddSingleton<IRabbitMqEnvelopeMapper, RabbitMqHeaderEnrich>();

        rabbit
            .ConfigureSenders(s => s.UseInterop(new RabbitMqHeaderEnrich()))   // all publishers
            .ConfigureListeners(l => l.UseInterop(new RabbitMqHeaderEnrich())); // all consumers

        if (setupOptions.UseDebugLogging)
        {
            // Enable detailed logging
            services.AddLogging(logging =>
            {
                logging.SetMinimumLevel(LogLevel.Debug);
                logging.AddConsole();
            });
        }

        //opts.PublishAllMessages()
        //    //.ToRabbitExchange("textmessage")
        //    .ToRabbitQueue("textmessage-queue")
        //    .UseInterop(new LegacyRabbitMapper()); // <-- endpoint-level

        if (setupOptions.DeclareExchanges)
        {
            rabbit.DeclareExchange("diagnostics", exchange =>
            {
                exchange.ExchangeType = ExchangeType.Topic;
                //exchange.IsDurable = true;
            });

            rabbit.DeclareExchange("textmessage", exchange =>
            {
                exchange.ExchangeType = ExchangeType.Topic;
                //exchange.IsDurable = true;
            });
        }

        var map = new ServiceCollection();
        //message.AddSingleton<TextMessage>();
        //message.AddSingleton<PingMessage>();
        //message.AddSingleton<HeartbeatMessage>();

        map.AddSingleton<MessageMap<TextMessage>>();
        map.AddSingleton<MessageMap<PingMessage>>();
        map.AddSingleton<MessageMap<HeartbeatMessage>>();

        foreach (var messageType in map)
        {
            //var inst = messageType.ImplementationInstance;
            //var m = inst as IMessageMap;

            var type = messageType.ImplementationType!;
            //var queueName = messageQueueNameRegistration.TryLookup(type);
            //if (queueName is null)
            //{
            //    queueName = "default-queue";
            //}

            var obj = Activator.CreateInstance(type) as IMessageMap;
            ArgumentNullException.ThrowIfNull(obj);
            // DomainException.ThrowIfNull(obj);
            var exchangeName = $"{obj.Exchange}";
            //rabbit.DeclareExchange(exchangeName, exchange =>
            //{
            //    exchange.ExchangeType = ExchangeType.Topic;
            //    exchange.IsDurable = basicOptions.Durable;
            //    exchange.AutoDelete = basicOptions.AutoDeleteExchange;
            //});

            //opts.ListenToRabbitQueue(queueName, queue =>
            //{
            //    queue.BindExchange(exchangeName, obj.BindingPattern);
            //    queue.IsDurable = basicOptions.Durable;
            //    queue.AutoDelete = basicOptions.AutoDeleteQueue;
            //    queue.PurgeOnStartup = basicOptions.PurgeOnStartup;
            //    queue.IsExclusive = basicOptions.Exclusive;
            //    queue.QueueType = QueueType.classic; //new streaming available
            //    //queue.DeadLetterQueue = new DeadLetterQueue()
            //    queue.TimeToLive(TimeSpan.FromSeconds(basicOptions.TimeToLive));
            //});
        }
        //opts.PublishMessage<TextMessage>()
        //    //.ToRabbitExchange("textmessage")
        //    .ToRabbitQueue("textmessage-queue")
        //    //.ToRabbitQueue("textmessage.#")
        //    ;

        //opts.PublishMessage<PingMessage>()
        //    .ToRabbitQueue("diagnostics-queue")
        //    //.ToRabbitQueue("diagnostics.#")
        //    ;

        //if (assemblies is not null)
        //{
        //    foreach (var assembly in assemblies)
        //    {
        //        opts.Discovery.IncludeAssembly(assembly);
        //    }
        //}

        opts.Discovery.IncludeAssembly(typeof(Messaging.Library.Configuration.Anchor).Assembly);
    }
}

public interface IMessageMap
{
    Type MessageType { get; }
    bool UseExchange { get; set; }
    bool UseQueue { get; set; }
    bool UseHeaderMapping { get; set; }
    string Exchange { get; }
    string BindingPattern { get; }
    string RoutingKey { get; }
    string? QueueName { get; }
}

public class MessageMap<T> : IMessageMap where T : IMessage, new()
{
    public Type MessageType => typeof(T);

    public bool UseExchange { get; set; } = true;
    public bool UseQueue { get; set; }
    public bool UseHeaderMapping { get; set; } = true;


    public string Exchange => new T().ExchangeName;
    public string BindingPattern => new T().BindingPattern;
    public string RoutingKey => new T().RoutingKey;
    public string? QueueName => new T().QueueName;
}


public class RabbitMqSetupOptions
{
    public bool DeclareExchanges { get; set; }
    public bool UseDebugLogging { get; set; } = true;

}
public class RabbitMqOptions
{
    public static string SectionName = "RabbitMqOptions";

    public string HostName { get; set; } = "127.0.0.1";
    public string VirtualHost { get; set; } = ConnectionFactory.DefaultVHost;
    public string UserName { get; set; } = ConnectionFactory.DefaultUser;
    public string Password { get; set; } = ConnectionFactory.DefaultPass;
    public int Port { get; set; } = 5672;

    public TimeSpan Heartbeat { get; set; } = ConnectionFactory.DefaultHeartbeat;
    public TimeSpan DefaultConnectionTimeout { get; set; } = ConnectionFactory.DefaultConnectionTimeout;

}