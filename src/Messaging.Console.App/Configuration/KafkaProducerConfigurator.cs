using Messaging.Domain.Library.Orders;
using Messaging.Domain.Library.Payments;

using Wolverine;
using Wolverine.Kafka;

namespace Messaging.Console.App.Configuration;

public static class KafkaProducerConfigurator
{
    public static void Build(WolverineOptions opts)
    {
        //opts.Policies.OnException<InvalidOperationException>()
        //    .RetryWithCooldown(maxAttempts: 3,
        //        cooldown: TimeSpan.FromSeconds(2),
        //        maxCooldown: TimeSpan.FromSeconds(10));

        opts.UseKafka("localhost:9094"); //default is 9092, so this is wired to the docker-compose setup

        // Simple topic publishing
        opts.PublishMessage<OrderCreated>().ToKafkaTopic("orders-created");
        opts.PublishMessage<OrderUpdated>().ToKafkaTopic("orders-updated");
        opts.PublishMessage<PaymentProcessed>().ToKafkaTopic("payments");

        // Discovery

        opts.Discovery.IncludeAssembly(typeof(Messaging.Domain.Library.Configuration.Anchor).Assembly);
    }
}