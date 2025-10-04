using Messaging.Domain.Library.Orders;
using Messaging.Domain.Library.Payments;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Wolverine;
using Wolverine.RabbitMQ;

namespace Messaging.Console.App.Configuration.RabbitMqSupport;

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
        opts.Discovery.IncludeAssembly(typeof(Domain.Library.Configuration.Anchor).Assembly);

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


}