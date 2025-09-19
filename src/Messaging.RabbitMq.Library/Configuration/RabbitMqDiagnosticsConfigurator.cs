using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Wolverine;
using Wolverine.RabbitMQ;

namespace Messaging.RabbitMq.Library.Configuration;

public static class RabbitMqDiagnosticsConfigurator
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


        rabbit
            .ConfigureSenders(s => s.UseInterop(new RabbitMqHeaderEnrich()))   // all publishers
            .ConfigureListeners(l => l.UseInterop(new RabbitMqHeaderEnrich())); // all consumers


        // Enable detailed logging
        opts.Services.AddLogging(logging =>
        {
            logging.SetMinimumLevel(LogLevel.Debug);
            ConsoleLoggerExtensions.AddConsole((ILoggingBuilder)logging);
        });

        opts.Discovery.IncludeAssembly(typeof(Messaging.RabbitMq.Library.Configuration.Anchor).Assembly);


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

        // Specific queue configuration
        opts.ListenToRabbitQueue("diagnostics-queue", queue =>
        {
            queue.BindExchange("diagnostics", "diagnostics.#");
            //queue.IsDurable = true;
            queue.PurgeOnStartup = false;
        });

        opts.ListenToRabbitQueue("textmessage-queue", queue =>
        {
            queue.BindExchange("textmessage", "textmessage.#");
            //queue.IsDurable = true; // Survives broker restart
            queue.PurgeOnStartup = false;
        });

    }
}