using Messaging.RabbitMq.Library;
using Messaging.RabbitMq.Library.Configuration;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Wolverine;
using Wolverine.RabbitMQ;
using Wolverine.RabbitMQ.Internal;

namespace Messaging.Console.App.Configuration;

public static class RabbitMqdDiagnosticsProducerConfigurator
{
    public static void BuildDiagnostics(WolverineOptions opts)
    {
        // Basic RabbitMQ connection
        var rabbit = opts.UseRabbitMq(rabbit =>
        {
            rabbit.HostName = "localhost";
            rabbit.Port = 5672;
            rabbit.UserName = "guest";
            rabbit.Password = "guest";
            rabbit.VirtualHost = "/"; // optional
            // Connection pool settings
            rabbit.RequestedHeartbeat = TimeSpan.FromSeconds(30);
            rabbit.RequestedConnectionTimeout = TimeSpan.FromSeconds(10);
        });

        rabbit.AutoProvision().AutoPurgeOnStartup();
        var services = opts.Services;

        services.AddSingleton<IRabbitMqEnvelopeMapper, RabbitMqHeaderEnrich>();

        rabbit
            .ConfigureSenders(s => s.UseInterop(new RabbitMqHeaderEnrich()))   // all publishers
            .ConfigureListeners(l => l.UseInterop(new RabbitMqHeaderEnrich())); // all consumers

        //opts.PublishAllMessages()
        //    //.ToRabbitExchange("textmessage")
        //    .ToRabbitQueue("textmessage-queue")
        //    .UseInterop(new LegacyRabbitMapper()); // <-- endpoint-level

        // Enable detailed logging
        opts.Services.AddLogging(logging =>
        {
            logging.SetMinimumLevel(LogLevel.Debug);
            logging.AddConsole();
        });

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


        opts.PublishMessage<TextMessage>()
            //.ToRabbitExchange("textmessage")
            .ToRabbitQueue("textmessage-queue")
            //.ToRabbitQueue("textmessage.#")
            ;

        opts.PublishMessage<PingMessage>()
            .ToRabbitQueue("diagnostics-queue")
            //.ToRabbitQueue("diagnostics.#")
            ;

        opts.Discovery.IncludeAssembly(typeof(Messaging.Library.Configuration.Anchor).Assembly);
    }
}