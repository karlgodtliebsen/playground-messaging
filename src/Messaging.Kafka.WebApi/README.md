# playground-messaging Kafka



### Docker commands to run Kafka

```shell

docker-compose up -d
```

# docker-compose.yml using kafak--ui
```shell
services:
  kafka:
    image: bitnami/kafka:3.8   # KRaft-enabled; no ZooKeeper needed
    container_name: kafka
    ports:
      - "9094:9094"           # external client access from your host
    environment:
      # --- KRaft single-node ---
      KAFKA_ENABLE_KRAFT: "yes"
      KAFKA_CFG_NODE_ID: "1"
      KAFKA_CFG_PROCESS_ROLES: "broker,controller"
      KAFKA_CFG_CONTROLLER_LISTENER_NAMES: "CONTROLLER"
      KAFKA_CFG_CONTROLLER_QUORUM_VOTERS: "1@kafka:29093"

      # --- Listeners / advertised listeners ---
      # Internal listener for other containers, controller listener, and an external listener for your host
      KAFKA_CFG_LISTENERS: "PLAINTEXT://:9092,CONTROLLER://:29093,EXTERNAL://:9094"
      KAFKA_CFG_ADVERTISED_LISTENERS: "PLAINTEXT://kafka:9092,EXTERNAL://localhost:9094"
      KAFKA_CFG_LISTENER_SECURITY_PROTOCOL_MAP: "PLAINTEXT:PLAINTEXT,EXTERNAL:PLAINTEXT,CONTROLLER:PLAINTEXT"
      KAFKA_CFG_INTER_BROKER_LISTENER_NAME: "PLAINTEXT"

      # --- Quality-of-life defaults (safe for local dev) ---
      KAFKA_CFG_OFFSETS_TOPIC_REPLICATION_FACTOR: "1"
      KAFKA_CFG_TRANSACTION_STATE_LOG_REPLICATION_FACTOR: "1"
      KAFKA_CFG_TRANSACTION_STATE_LOG_MIN_ISR: "1"
      KAFKA_CFG_AUTO_CREATE_TOPICS_ENABLE: "true"
      KAFKA_CFG_NUM_PARTITIONS: "3"      # default for new topics

      # --- Misc ---
      ALLOW_PLAINTEXT_LISTENER: "yes"

    volumes:
      - kafka-data:/bitnami/kafka   # persists local data between restarts

  kafka-ui:
    image: provectuslabs/kafka-ui:latest
    container_name: kafka-ui
    depends_on:
      - kafka
    ports:
      - "8080:8080"
    environment:
      KAFKA_CLUSTERS_0_NAME: "local"
      KAFKA_CLUSTERS_0_BOOTSTRAPSERVERS: "kafka:9092"  # container-to-container
      KAFKA_CLUSTERS_0_READONLY: "false"

volumes:
  kafka-data:

```
# docker-compose.yml using redpanda

```shell
version: "3.9"

services:
  kafka:
    image: bitnami/kafka:3.8   # KRaft-enabled; no ZooKeeper needed
    container_name: kafka
    ports:
      - "9094:9094"           # external client access from your host
    environment:
      # --- KRaft single-node ---
      KAFKA_ENABLE_KRAFT: "yes"
      KAFKA_CFG_NODE_ID: "1"
      KAFKA_CFG_PROCESS_ROLES: "broker,controller"
      KAFKA_CFG_CONTROLLER_LISTENER_NAMES: "CONTROLLER"
      KAFKA_CFG_CONTROLLER_QUORUM_VOTERS: "1@kafka:29093"

      # --- Listeners / advertised listeners ---
      KAFKA_CFG_LISTENERS: "PLAINTEXT://:9092,CONTROLLER://:29093,EXTERNAL://:9094"
      KAFKA_CFG_ADVERTISED_LISTENERS: "PLAINTEXT://kafka:9092,EXTERNAL://localhost:9094"
      KAFKA_CFG_LISTENER_SECURITY_PROTOCOL_MAP: "PLAINTEXT:PLAINTEXT,EXTERNAL:PLAINTEXT,CONTROLLER:PLAINTEXT"
      KAFKA_CFG_INTER_BROKER_LISTENER_NAME: "PLAINTEXT"

      # --- Quality-of-life defaults ---
      KAFKA_CFG_OFFSETS_TOPIC_REPLICATION_FACTOR: "1"
      KAFKA_CFG_TRANSACTION_STATE_LOG_REPLICATION_FACTOR: "1"
      KAFKA_CFG_TRANSACTION_STATE_LOG_MIN_ISR: "1"
      KAFKA_CFG_AUTO_CREATE_TOPICS_ENABLE: "true"
      KAFKA_CFG_NUM_PARTITIONS: "3"

      # --- Misc ---
      ALLOW_PLAINTEXT_LISTENER: "yes"

    volumes:
      - kafka-data:/bitnami/kafka

  redpanda-console:
    image: redpandadata/console:latest
    container_name: redpanda-console
    depends_on:
      - kafka
    ports:
      - "8080:8080"
    environment:
      KAFKA_BROKERS: "kafka:9092"

volumes:
  kafka-data:

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