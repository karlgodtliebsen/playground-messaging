namespace Messaging.PostgreSql.Library.Configuration;

public static class PostgreSqlConfigurationBuilder
{
    //public static void BuildProducer(WolverineOptions opts)
    //{
    //    //opts.Policies.OnException<InvalidOperationException>()
    //    //    .RetryWithCooldown(maxAttempts: 3,
    //    //        cooldown: TimeSpan.FromSeconds(2),
    //    //        maxCooldown: TimeSpan.FromSeconds(10));

    //    var kafka = opts.UseKafka("localhost:9094"); //default is 9092, so this is wired to the docker-compose setup

    //    //// With consumer group and client ID
    //    //opts.UseKafka("localhost:9094;group.id=my-consumer-group;client.id=my-app");

    //    //// Production with multiple brokers
    //    //opts.UseKafka("broker1:9094,broker2:9094,broker3:9094");

    //    //// With SSL/SASL (production)
    //    //opts.UseKafka("broker:9094;security.protocol=SASL_SSL;sasl.mechanism=PLAIN;sasl.username=user;sasl.password=pass");

    //    BuildProducer(opts, kafka);
    //    // Discovery

    //    opts.Discovery.IncludeAssembly(typeof(Messaging.Kafka.Library.Configuration.Anchor).Assembly);
    //    opts.Discovery.IncludeAssembly(typeof(Messaging.Domain.Library.Configuration.Anchor).Assembly);
    //}
    //public static void BuildProducer(WolverineOptions opts, KafkaTransportExpression kafka)
    //{
    //    var services = opts.Services;
    //    kafka.AutoProvision();
    //    //Debug logging
    //    services.AddLogging(logging =>
    //    {
    //        logging.SetMinimumLevel(LogLevel.Debug);
    //        logging.AddConsole();
    //    });
    //    kafka.AutoPurgeOnStartup();

    //    // Simple topic publishing

    //    opts.PublishMessage<OrderCreated>().ToKafkaTopic("orders");//.ConfigureProducer();
    //    opts.PublishMessage<OrderUpdated>().ToKafkaTopic("orders");
    //    opts.PublishMessage<PaymentProcessed>().ToKafkaTopic("payments");

    //    opts.PublishMessage<CreateMessage>().ToKafkaTopic("messages");
    //    opts.PublishMessage<InformationMessage>().ToKafkaTopic("messages");

    //    opts.PublishMessage<PingMessage>().ToKafkaTopic("diagnostics-messages");
    //    opts.PublishMessage<HeartbeatMessage>().ToKafkaTopic("diagnostics-messages");
    //    opts.PublishMessage<TextMessage>().ToKafkaTopic("diagnostics-messages");

    //    // Discovery
    //    opts.Discovery.IncludeAssembly(typeof(Messaging.Kafka.Library.Configuration.Anchor).Assembly);
    //    opts.Discovery.IncludeAssembly(typeof(Messaging.Domain.Library.Configuration.Anchor).Assembly);
    //}



    //public static void BuildConsumer(WolverineOptions opts)
    //{
    //    //opts.Policies.OnException<InvalidOperationException>()
    //    //    .RetryWithCooldown(maxAttempts: 3,
    //    //        cooldown: TimeSpan.FromSeconds(2),
    //    //        maxCooldown: TimeSpan.FromSeconds(10));

    //    var kafka = opts.UseKafka("localhost:9094"); //default is 9092, so this is wired to the docker-compose setup

    //    //variations
    //    // With consumer group and client ID
    //    //var kafka = opts.UseKafka("localhost:9094;group.id=my-consumer-group;client.id=my-app");
    //    // Production with multiple brokers
    //    //var kafka = opts.UseKafka("broker1:9094,broker2:9094,broker3:9094");

    //    //// With SSL/SASL (production)
    //    //var kafka = opts.UseKafka("broker:9094;security.protocol=SASL_SSL;sasl.mechanism=PLAIN;sasl.username=user;sasl.password=pass");

    //    BuildConsumer(opts, kafka);
    //}

    //public static void BuildConsumer(WolverineOptions opts, KafkaTransportExpression kafka)
    //{
    //    // Discovery
    //    opts.Discovery.IncludeAssembly(typeof(Messaging.Kafka.Library.Configuration.Anchor).Assembly);
    //    opts.Discovery.IncludeAssembly(typeof(Messaging.Domain.Library.Configuration.Anchor).Assembly);

    //    var services = opts.Services;
    //    kafka.AutoProvision();
    //    //Debug logging
    //    services.AddLogging(logging =>
    //    {
    //        logging.SetMinimumLevel(LogLevel.Debug);
    //        logging.AddConsole();
    //    });
    //    const string consumerGroup = "messaging-group";
    //    // Listen to topics (simple syntax)

    //    opts.ListenToKafkaTopic("orders");
    //    opts.ListenToKafkaTopic("payments");
    //    opts.ListenToKafkaTopic("messages");
    //    opts.ListenToKafkaTopic("diagnostics-messages");

    //    //opts.ListenToKafkaTopic("orders")
    //    //    .ProcessInline()
    //    //    .ConfigureConsumer(consumer =>
    //    //    {
    //    //        consumer.AutoOffsetReset = AutoOffsetReset.Earliest;
    //    //        consumer.GroupId = consumerGroup + "-orders";
    //    //        consumer.EnableAutoCommit = true;
    //    //        consumer.AutoCommitIntervalMs = 5000;
    //    //    })
    //    //    ;
    //    //opts.ListenToKafkaTopic("payments")
    //    //    .ProcessInline()
    //    //    .ConfigureConsumer(consumer =>
    //    //    {
    //    //        // Start from earliest available messages
    //    //        consumer.GroupId = consumerGroup + "-payments";
    //    //        consumer.AutoOffsetReset = AutoOffsetReset.Earliest;
    //    //        consumer.EnableAutoCommit = true;
    //    //        consumer.AutoCommitIntervalMs = 5000;
    //    //    });

    //    //opts.ListenToKafkaTopic("messages")
    //    //    .ProcessInline()
    //    //    .ConfigureConsumer(consumer =>
    //    //    {
    //    //        // Start from earliest available messages
    //    //        consumer.GroupId = consumerGroup + "-messages";
    //    //        consumer.AutoOffsetReset = AutoOffsetReset.Earliest;
    //    //        consumer.EnableAutoCommit = true;
    //    //        consumer.AutoCommitIntervalMs = 5000;
    //    //    });

    //    //opts.ListenToKafkaTopic("diagnostics-messages")
    //    //    //.ProcessInline()
    //    //    .ConfigureConsumer(consumer =>
    //    //    {
    //    //        // Start from earliest available messages
    //    //        consumer.AutoOffsetReset = AutoOffsetReset.Earliest;
    //    //        consumer.GroupId = consumerGroup + "-diagnostics-messages";
    //    //        consumer.EnableAutoCommit = true;
    //    //        consumer.AutoCommitIntervalMs = 5000;
    //    //    });

    //}

    //public static void BuildCombined(WolverineOptions opts)
    //{
    //    var kafka = opts.UseKafka("localhost:9094"); //default is 9092, so this is wired to the docker-compose setup
    //    BuildProducer(opts, kafka);
    //    BuildConsumer(opts, kafka);
    //}

}