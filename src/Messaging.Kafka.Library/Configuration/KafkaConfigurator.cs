using Messaging.Library.Orders;
using Messaging.Library.Payments;
using Wolverine;
using Wolverine.Kafka;

namespace Messaging.Kafka.Library.Configuration;

public static class KafkaConfigurator
{
    public static void Build(WolverineOptions opts)
    {
        //opts.Policies.OnException<InvalidOperationException>()
        //    .RetryWithCooldown(maxAttempts: 3,
        //        cooldown: TimeSpan.FromSeconds(2),
        //        maxCooldown: TimeSpan.FromSeconds(10));

        opts.UseKafka("localhost:9094"); //default is 9092, so this is wired to the docker-compose setup

        //// With consumer group and client ID
        //opts.UseKafka("localhost:9094;group.id=my-consumer-group;client.id=my-app");

        //// Production with multiple brokers
        //opts.UseKafka("broker1:9094,broker2:9094,broker3:9094");

        //// With SSL/SASL (production)
        //opts.UseKafka("broker:9094;security.protocol=SASL_SSL;sasl.mechanism=PLAIN;sasl.username=user;sasl.password=pass");

        // Topic configuration and message routing

        // Simple topic publishing

        opts.PublishMessage<OrderCreated>().ToKafkaTopic("orders-created");
        opts.PublishMessage<OrderUpdated>().ToKafkaTopic("orders-updated");
        opts.PublishMessage<PaymentProcessed>().ToKafkaTopic("payments");


        // Listen to topics (simple syntax)
        opts.ListenToKafkaTopic("orders-created");
        opts.ListenToKafkaTopic("orders-updated");
        opts.ListenToKafkaTopic("payments");

        // Discovery

        opts.Discovery.IncludeAssembly(typeof(Messaging.Kafka.Library.Configuration.Anchor).Assembly);
        opts.Discovery.IncludeAssembly(typeof(Messaging.Library.Configuration.Anchor).Assembly);
    }
}