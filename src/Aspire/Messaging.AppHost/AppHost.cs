var builder = DistributedApplication.CreateBuilder(args);


builder.AddProject<Projects.Messaging_Kafka_WebApi>("messaging-kafka-webapi");


builder.AddProject<Projects.Messaging_RabbitMq_WebApi>("messaging-rabbitmq-webapi");


builder.Build().Run();
