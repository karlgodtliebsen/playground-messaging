using Messaging.Library.Orders;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Wolverine;
using Wolverine.RabbitMQ;

namespace Messaging.RabbitMq.Library.Configuration;

public static class RabbitMqConfigurator
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
        //rabbit.AutoPurgeOnStartup();
        //rabbit.PrefixIdentifiersWithMachineName();
        //rabbit.PrefixIdentifiers("msg");

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
        opts.Discovery.IncludeAssembly(typeof(Messaging.RabbitMq.Library.Configuration.Anchor).Assembly);
        opts.Discovery.IncludeAssembly(typeof(Messaging.Library.Configuration.Anchor).Assembly);


        // Configure exchanges and queues
        opts.PublishAllMessages()
            .ToRabbitExchange("orders-exchange")
            ;

        //ToRabbitRoutingKey
        //ToRabbitRoutingKeyOnNamedBroker
        //ToRabbitTopics


        // Specific queue configuration
        opts.ListenToRabbitQueue("order-processing-queue", queue =>
        {
            queue.BindExchange("orders-exchange", "orders.*");
            queue.IsDurable = true; // Survives broker restart
            //queue.AutoDelete = true;
            queue.PurgeOnStartup = false;
        });

        // Complex routing with multiple exchanges
        opts.ListenToRabbitQueue("payment-queue", queue =>
        {
            queue.BindExchange("payments-exchange", "payment.processed");
            queue.BindExchange("payments-exchange", "payment.failed");
            queue.IsDurable = true;
            queue.Arguments.Add("x-message-ttl", 300000); // 5 minutes TTL
            queue.Arguments.Add("x-max-priority", 10);
        });


        // Dead letter queue setup
        opts.ListenToRabbitQueue("failed-orders-queue", queue =>
        {
            queue.BindExchange("orders-exchange", "orders.failed");
            queue.IsDurable = true;
            queue.TimeToLive(TimeSpan.FromDays(7));
        });

        // Route specific messages to specific queues
        opts.PublishMessage<OrderCreated>()
            .ToRabbitQueue("order-processing-queue")
            //  .WithHeader("MessageType", "OrderCreated")
            // .WithHeader("Priority", "Normal")
            ;

        opts.PublishMessage<UrgentOrderCreated>()
            .ToRabbitQueue("urgent-orders-queue")
            //.WithHeader("Priority", "High")
            //.WithHeader("SLA", "15min")
            ;

        //// Fanout exchange for broadcasting
        //opts.PublishMessage<PaymentProcessed>()
        //    .ToRabbitExchange("notifications-fanout", ExchangeType.Fanout);

        //// Configure retry policies
        //opts.Policies.OnException<InvalidOperationException>()
        //    .RetryWithCooldown(3, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(5));
    }
}