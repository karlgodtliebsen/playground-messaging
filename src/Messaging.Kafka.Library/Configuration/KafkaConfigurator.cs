using Messaging.Domain.Library.DemoMessages;
using Messaging.Domain.Library.Orders;
using Messaging.Domain.Library.Payments;
using Messaging.Domain.Library.SimpleMessages;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Wolverine;
using Wolverine.Kafka;

namespace Messaging.Kafka.Library.Configuration;

public static class KafkaConfigurator
{


    public static void BuildProducer(WolverineOptions opts)
    {
        //opts.Policies.OnException<InvalidOperationException>()
        //    .RetryWithCooldown(maxAttempts: 3,
        //        cooldown: TimeSpan.FromSeconds(2),
        //        maxCooldown: TimeSpan.FromSeconds(10));

        var kafka = opts.UseKafka("localhost:9094"); //default is 9092, so this is wired to the docker-compose setup

        //// With consumer group and client ID
        //opts.UseKafka("localhost:9094;group.id=my-consumer-group;client.id=my-app");

        //// Production with multiple brokers
        //opts.UseKafka("broker1:9094,broker2:9094,broker3:9094");

        //// With SSL/SASL (production)
        //opts.UseKafka("broker:9094;security.protocol=SASL_SSL;sasl.mechanism=PLAIN;sasl.username=user;sasl.password=pass");

        BuildProducer(opts, kafka);
        // Discovery

        opts.Discovery.IncludeAssembly(typeof(Messaging.Kafka.Library.Configuration.Anchor).Assembly);
        opts.Discovery.IncludeAssembly(typeof(Messaging.Domain.Library.Configuration.Anchor).Assembly);
    }
    public static void BuildProducer(WolverineOptions opts, KafkaTransportExpression kafka)
    {
        var services = opts.Services;
        kafka.AutoProvision();
        //Debug logging
        services.AddLogging(logging =>
        {
            logging.SetMinimumLevel(LogLevel.Debug);
            logging.AddConsole();
        });
        kafka.AutoPurgeOnStartup();

        // Simple topic publishing

        opts.PublishMessage<OrderCreated>().ToKafkaTopic("orders-created");
        opts.PublishMessage<OrderUpdated>().ToKafkaTopic("orders-updated");
        opts.PublishMessage<PaymentProcessed>().ToKafkaTopic("payments");

        opts.PublishMessage<CreateMessage>().ToKafkaTopic("create-message");
        opts.PublishMessage<InformationMessage>().ToKafkaTopic("information-message");

        opts.PublishMessage<PingMessage>().ToKafkaTopic("diagnostics-message");
        opts.PublishMessage<HeartbeatMessage>().ToKafkaTopic("diagnostics-message");
        opts.PublishMessage<TextMessage>().ToKafkaTopic("diagnostics-message");

        // Discovery
        opts.Discovery.IncludeAssembly(typeof(Messaging.Kafka.Library.Configuration.Anchor).Assembly);
        opts.Discovery.IncludeAssembly(typeof(Messaging.Domain.Library.Configuration.Anchor).Assembly);
    }



    public static void BuildConsumer(WolverineOptions opts)
    {
        //opts.Policies.OnException<InvalidOperationException>()
        //    .RetryWithCooldown(maxAttempts: 3,
        //        cooldown: TimeSpan.FromSeconds(2),
        //        maxCooldown: TimeSpan.FromSeconds(10));

        var kafka = opts.UseKafka("localhost:9094"); //default is 9092, so this is wired to the docker-compose setup

        //variations
        // With consumer group and client ID
        //var kafka = opts.UseKafka("localhost:9094;group.id=my-consumer-group;client.id=my-app");
        // Production with multiple brokers
        //var kafka = opts.UseKafka("broker1:9094,broker2:9094,broker3:9094");

        //// With SSL/SASL (production)
        //var kafka = opts.UseKafka("broker:9094;security.protocol=SASL_SSL;sasl.mechanism=PLAIN;sasl.username=user;sasl.password=pass");

        BuildConsumer(opts, kafka);
    }

    public static void BuildConsumer(WolverineOptions opts, KafkaTransportExpression kafka)
    {
        // Discovery
        opts.Discovery.IncludeAssembly(typeof(Messaging.Kafka.Library.Configuration.Anchor).Assembly);
        opts.Discovery.IncludeAssembly(typeof(Messaging.Domain.Library.Configuration.Anchor).Assembly);

        var services = opts.Services;
        kafka.AutoProvision();
        //Debug logging
        services.AddLogging(logging =>
        {
            logging.SetMinimumLevel(LogLevel.Debug);
            logging.AddConsole();
        });
        // Listen to topics (simple syntax)
        opts.ListenToKafkaTopic("orders-created");
        opts.ListenToKafkaTopic("orders-updated");
        opts.ListenToKafkaTopic("payments");

        opts.ListenToKafkaTopic("create-message");
        opts.ListenToKafkaTopic("information-message");

        opts.ListenToKafkaTopic("diagnostics-message");

    }

    public static void BuildCombined(WolverineOptions opts)
    {
        var kafka = opts.UseKafka("localhost:9094"); //default is 9092, so this is wired to the docker-compose setup
        BuildProducer(opts, kafka);
        BuildConsumer(opts, kafka);
    }

}