using Confluent.Kafka;

using Messaging.Domain.Library.DemoMessages;
using Messaging.Domain.Library.Orders;
using Messaging.Domain.Library.Payments;
using Messaging.Domain.Library.SimpleMessages;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Wolverine;
using Wolverine.Kafka;

namespace Messaging.Kafka.Library.Configuration;

public static class KafkaConfigurationBuilder
{
    public static void BuildTestProducer(WolverineOptions opts)
    {
        var services = opts.Services;
        var sp = services.BuildServiceProvider();
        var options = sp.GetRequiredService<IOptions<KafkaOptions>>().Value;
        var hostPortName = $"{options.HostName}:{options.Port}";
        //var hostPortNameWithConsumerName = $"{options.HostName}:{options.Port};group.id={options.ConsumerGroup};client.id={options.ClientId}";

        var kafka = opts.UseKafka(hostPortName)
            .ConfigureClient(client =>
            {
                // These are important for development
                client.SecurityProtocol = SecurityProtocol.Plaintext;
                client.Acks = Acks.All; // Wait for all replicas
                client.Debug = "broker,topic,msg"; // Enable debug logging temporarily
            });
        opts.PublishMessage<PingMessage>().ToKafkaTopic("diagnostics-messages").DeliverWithin(TimeSpan.FromSeconds(30));
        opts.PublishMessage<HeartbeatMessage>().ToKafkaTopic("diagnostics-messages").DeliverWithin(TimeSpan.FromSeconds(30));
        opts.PublishMessage<TextMessage>().ToKafkaTopic("diagnostics-messages").DeliverWithin(TimeSpan.FromSeconds(30));

        opts.Discovery.IncludeAssembly(typeof(Messaging.Kafka.Library.Configuration.Anchor).Assembly);
        opts.Discovery.IncludeAssembly(typeof(Messaging.Domain.Library.Configuration.Anchor).Assembly);
    }

    //opts.Policies.OnException<InvalidOperationException>()
    //    .RetryWithCooldown(maxAttempts: 3,
    //        cooldown: TimeSpan.FromSeconds(2),
    //        maxCooldown: TimeSpan.FromSeconds(10));
    //// With consumer group and client ID
    //opts.UseKafka("localhost:9094;group.id=my-consumer-group;client.id=my-app");

    //// Production with multiple brokers
    //opts.UseKafka("broker1:9094,broker2:9094,broker3:9094");

    //// With SSL/SASL (production)
    //opts.UseKafka("broker:9094;security.protocol=SASL_SSL;sasl.mechanism=PLAIN;sasl.username=user;sasl.password=pass");
    public static void BuildProducer(WolverineOptions opts)
    {

        var services = opts.Services;
        var sp = services.BuildServiceProvider();
        var options = sp.GetRequiredService<IOptions<KafkaOptions>>().Value;
        var hostPortName = $"{options.HostName}:{options.Port}";
        var kafka = opts.UseKafka(hostPortName).ConfigureClient(client =>
        {
            // These are for development
            client.SecurityProtocol = SecurityProtocol.Plaintext;
            client.Acks = Acks.All; // Wait for all replicas
            //client.MessageTimeoutMs = 30000; // 30 second timeout
            //client.RequestTimeoutMs = 30000;
            //client.LingerMs = 100; // Wait up to 100ms to batch messages
            client.Debug = "broker,topic,msg"; // Enable debug logging temporarily
        });

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
            logging.SetMinimumLevel(LogLevel.Trace);
            logging.AddConsole();
        });
        kafka.AutoPurgeOnStartup();

        // Simple topic publishing

        opts.PublishMessage<OrderCreated>().ToKafkaTopic("orders").DeliverWithin(TimeSpan.FromSeconds(30));
        opts.PublishMessage<OrderUpdated>().ToKafkaTopic("orders").DeliverWithin(TimeSpan.FromSeconds(30));
        opts.PublishMessage<PaymentProcessed>().ToKafkaTopic("payments").DeliverWithin(TimeSpan.FromSeconds(30));

        opts.PublishMessage<CreateMessage>().ToKafkaTopic("messages").DeliverWithin(TimeSpan.FromSeconds(30));
        opts.PublishMessage<InformationMessage>().ToKafkaTopic("messages").DeliverWithin(TimeSpan.FromSeconds(30));

        opts.PublishMessage<PingMessage>().ToKafkaTopic("diagnostics-messages").DeliverWithin(TimeSpan.FromSeconds(30));
        opts.PublishMessage<HeartbeatMessage>().ToKafkaTopic("diagnostics-messages").DeliverWithin(TimeSpan.FromSeconds(30));
        opts.PublishMessage<TextMessage>().ToKafkaTopic("diagnostics-messages").DeliverWithin(TimeSpan.FromSeconds(30));

        // Discovery
        opts.Discovery.IncludeAssembly(typeof(Messaging.Kafka.Library.Configuration.Anchor).Assembly);
        opts.Discovery.IncludeAssembly(typeof(Messaging.Domain.Library.Configuration.Anchor).Assembly);
    }



    public static void BuildConsumer(WolverineOptions opts)
    {
        var services = opts.Services;
        var sp = services.BuildServiceProvider();
        var options = sp.GetRequiredService<IOptions<KafkaOptions>>().Value;
        //var hostPortNameWithConsumerName = $"{options.HostName}:{options.Port};group.id={options.ConsumerGroup};client.id={options.ClientId}";
        var hostPortName = $"{options.HostName}:{options.Port}";
        var kafka = opts.UseKafka(hostPortName).ConfigureClient(client =>
        {
            // These are for development
            client.SecurityProtocol = SecurityProtocol.Plaintext;
            client.Acks = Acks.All;
            client.Debug = "broker,topic,msg"; // Enable debug logging temporarily
        });
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
            logging.SetMinimumLevel(LogLevel.Trace);
            logging.AddConsole();
        });
        const string consumerGroup = "messaging-group";
        // Listen to topics (simple syntax)

        opts.ListenToKafkaTopic("orders");
        opts.ListenToKafkaTopic("payments");
        opts.ListenToKafkaTopic("messages");
        opts.ListenToKafkaTopic("diagnostics-messages");

        //opts.ListenToKafkaTopic("orders")
        //    .ProcessInline()
        //    .ConfigureConsumer(consumer =>
        //    {
        //        consumer.AutoOffsetReset = AutoOffsetReset.Earliest;
        //        consumer.GroupId = consumerGroup + "-orders";
        //        consumer.EnableAutoCommit = true;
        //        consumer.AutoCommitIntervalMs = 5000;
        //    })
        //    ;
        //opts.ListenToKafkaTopic("payments")
        //    .ProcessInline()
        //    .ConfigureConsumer(consumer =>
        //    {
        //        // Start from earliest available messages
        //        consumer.GroupId = consumerGroup + "-payments";
        //        consumer.AutoOffsetReset = AutoOffsetReset.Earliest;
        //        consumer.EnableAutoCommit = true;
        //        consumer.AutoCommitIntervalMs = 5000;
        //    });

        //opts.ListenToKafkaTopic("messages")
        //    .ProcessInline()
        //    .ConfigureConsumer(consumer =>
        //    {
        //        // Start from earliest available messages
        //        consumer.GroupId = consumerGroup + "-messages";
        //        consumer.AutoOffsetReset = AutoOffsetReset.Earliest;
        //        consumer.EnableAutoCommit = true;
        //        consumer.AutoCommitIntervalMs = 5000;
        //    });

        //opts.ListenToKafkaTopic("diagnostics-messages")
        //    //.ProcessInline()
        //    .ConfigureConsumer(consumer =>
        //    {
        //        // Start from earliest available messages
        //        consumer.AutoOffsetReset = AutoOffsetReset.Earliest;
        //        consumer.GroupId = consumerGroup + "-diagnostics-messages";
        //        consumer.EnableAutoCommit = true;
        //        consumer.AutoCommitIntervalMs = 5000;
        //    });

    }

    public static void BuildCombined(WolverineOptions opts)
    {
        var services = opts.Services;
        var sp = services.BuildServiceProvider();
        var options = sp.GetRequiredService<IOptions<KafkaOptions>>().Value;
        //var hostPortNameWithConsumerName = $"{options.HostName}:{options.Port};group.id={options.ConsumerGroup};client.id={options.ClientId}";
        var hostPortName = $"{options.HostName}:{options.Port}";
        var kafka = opts.UseKafka(hostPortName); //default is 9092, so this is wired to the docker-compose setup
        BuildProducer(opts, kafka);
        BuildConsumer(opts, kafka);
    }

}