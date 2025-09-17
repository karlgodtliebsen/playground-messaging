using Messaging.Library.Orders;
using Messaging.Library.Payments;
using Messaging.RabbitMq.Library;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Wolverine;
using Wolverine.Kafka;
using Wolverine.RabbitMQ;

namespace Messaging.Console.App.Configuration;

public static class RabbitMqProducerConfigurator
{
    public static void Build(WolverineOptions opts)
    {
        // Basic RabbitMQ connection
        var rabbit = opts.UseRabbitMq(rabbit =>
        {
            rabbit.HostName = "localhost";
            rabbit.Port = 5673;
            rabbit.UserName = "guest";
            rabbit.Password = "guest";
            rabbit.VirtualHost = "/"; // optional
            // Connection pool settings
            rabbit.RequestedHeartbeat = TimeSpan.FromSeconds(30);
            rabbit.RequestedConnectionTimeout = TimeSpan.FromSeconds(10);
        });

        rabbit.AutoProvision();

        // Enable detailed logging
        opts.Services.AddLogging(logging =>
        {
            logging.SetMinimumLevel(LogLevel.Debug);
            logging.AddConsole();
        });

        rabbit.DeclareExchange("orders-exchange", exchange =>
        {
            exchange.ExchangeType = ExchangeType.Topic;
            exchange.IsDurable = true;
        });
        opts.Discovery.IncludeAssembly(typeof(Messaging.Library.Configuration.Anchor).Assembly);

        // Configure exchanges and queues
        opts.PublishAllMessages().ToRabbitExchange("orders-exchange");

        // Route specific messages to specific queues
        opts.PublishMessage<OrderCreated>()
            .ToRabbitQueue("order-processing-queue")
            ;
        opts.PublishMessage<OrderUpdated>()
            .ToRabbitQueue("order-processing-queue")
            ;
        opts.PublishMessage<PaymentProcessed>()
            .ToRabbitQueue("payments-queue")
            ;
        opts.PublishMessage<UrgentOrderCreated>()
            .ToRabbitQueue("urgent-orders-queue")
            ;
    }

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

        rabbit.AutoProvision();

        // Enable detailed logging
        opts.Services.AddLogging(logging =>
        {
            logging.SetMinimumLevel(LogLevel.Debug);
            logging.AddConsole();
        });

        //rabbit.DeclareExchange("go.main.diagnostics", exchange =>
        //{
        //    exchange.ExchangeType = ExchangeType.Topic;
        //    //exchange.IsDurable = true;
        //});

        //rabbit.DeclareExchange("go.main.textmessage", exchange =>
        //{
        //    exchange.ExchangeType = ExchangeType.Topic;
        //    //exchange.IsDurable = true;
        //});


        opts.PublishMessage<TextMessage>()
            .ToRabbitQueue("nextgen")
            //.ToRabbitQueue("textmessage.#")
            ;

        opts.PublishMessage<PingMessage>()
            .ToRabbitQueue("nextgen")
            //.ToRabbitQueue("diagnostics.#")
            ;

        opts.Discovery.IncludeAssembly(typeof(Messaging.Library.Configuration.Anchor).Assembly);
    }
}