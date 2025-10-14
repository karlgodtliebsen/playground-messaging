var builder = DistributedApplication.CreateBuilder(args);




builder.AddProject<Projects.Messaging_Console_Kafka_Producer_App>("messaging-console-kafka-producer-app");




builder.Build().Run();
