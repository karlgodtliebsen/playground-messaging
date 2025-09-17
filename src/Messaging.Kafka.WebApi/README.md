# playground-messaging Kafka



### Docker commands to run Kafka

```shell

# docker-compose.yml
   version: '3'
   services:
     zookeeper:
       image: confluentinc/cp-zookeeper:latest
       environment:
         ZOOKEEPER_CLIENT_PORT: 2181
         ZOOKEEPER_TICK_TIME: 2000

     kafka:
       image: confluentinc/cp-kafka:latest
       depends_on:
         - zookeeper
       ports:
         - 9092:9092
       environment:
         KAFKA_BROKER_ID: 1
         KAFKA_ZOOKEEPER_CONNECT: zookeeper:2181
         KAFKA_ADVERTISED_LISTENERS: PLAINTEXT://localhost:9092
         KAFKA_OFFSETS_TOPIC_REPLICATION_FACTOR: 1

```


### Look into:

```csharp
/ ===========================================
// Advanced Kafka Configuration Examples
// ===========================================

/*
// Advanced configuration with multiple environments
builder.Host.UseWolverine(opts =>
{
    var kafkaConfig = builder.Configuration.GetSection("Kafka");
    
    opts.UseKafka(kafka =>
    {
        kafka.BootstrapServers = kafkaConfig["BootstrapServers"] ?? "localhost:9092";
        kafka.ClientId = kafkaConfig["ClientId"] ?? "wolverine-app";
        kafka.GroupId = kafkaConfig["GroupId"] ?? "wolverine-consumers";
        
        // SSL/SASL configuration for production
        if (kafkaConfig.GetValue<bool>("EnableSsl"))
        {
            kafka.SecurityProtocol = SecurityProtocol.SaslSsl;
            kafka.SslCaLocation = kafkaConfig["SslCaLocation"];
            kafka.SaslMechanism = SaslMechanism.Plain;
            kafka.SaslUsername = kafkaConfig["SaslUsername"];
            kafka.SaslPassword = kafkaConfig["SaslPassword"];
        }
        
        // Producer optimizations
        kafka.ProducerConfig.Acks = Acks.All;
        kafka.ProducerConfig.Retries = int.MaxValue;
        kafka.ProducerConfig.MaxInFlight = 5;
        kafka.ProducerConfig.EnableIdempotence = true;
        kafka.ProducerConfig.CompressionType = CompressionType.Snappy;
        
        // Consumer optimizations  
        kafka.ConsumerConfig.FetchMinBytes = 1024;
        kafka.ConsumerConfig.FetchMaxWaitMs = 500;
        kafka.ConsumerConfig.MaxPollRecords = 1000;
    });
    
    // Dead letter topic configuration
    opts.PublishMessage<OrderCreated>()
        .ToKafkaTopic("orders-created")
        .WithDeadLetterTopic("orders-created-dlq");
    
    // Batch processing
    opts.ListenToKafkaTopic("bulk-orders", topic =>
    {
        topic.ProcessInBatches(batchSize: 100, timeoutInSeconds: 30);
        topic.CreateIfMissing();
    });
    
    // Multiple consumer groups for the same topic
    opts.ListenToKafkaTopic("orders-created", "analytics-group", topic =>
    {
        topic.CreateIfMissing();
    });
    
    opts.ListenToKafkaTopic("orders-created", "notifications-group", topic =>
    {
        topic.CreateIfMissing();
    });
});
```