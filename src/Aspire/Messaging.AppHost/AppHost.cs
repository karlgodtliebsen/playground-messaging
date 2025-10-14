var builder = DistributedApplication.CreateBuilder(args);

/*
// docker compose -f docker-compose-kafka.yml up -d

var seq = builder.AddContainer("seq", "datalust/seq", "latest")
    .WithEnvironment("ACCEPT_EULA", "Y")
    .WithEnvironment("SEQ_API_CANONICALURI", "http://seq:80")
    .WithHttpEndpoint(port: 8041, targetPort: 80, name: "ui")
    ;

seq.WithUrlForEndpoint("ui", url =>
{
    url.DisplayText = "Seq UI";
    url.Url = "/events?range=1d";
});

var redPanda = builder.AddContainer("redpanda-console", "redpandadata/console", "latest")
    .WithHttpEndpoint(port: 8042, targetPort: 80, name: "RedPanda")
    ;

redPanda.WithUrlForEndpoint("RedPanda", url =>
{
    url.DisplayText = "RedPanda UI";
    url.Url = "/overview";
});

*/

//NOTE: notice the port number. This is wired to default kafka 9094 with a broker at 9092

var w = builder.AddProject<Projects.Messaging_Console_Kafka_Producer_App>("messaging-console-kafka-producer-app")
    .WithEnvironment("DOTNET_ENVIRONMENT", "Development")
    .WithEnvironment("KafkaOptions__HostName", "localhost")
    .WithEnvironment("KafkaOptions__Port", "9094")

    //this is more of a demonstration of "this is also possible". 
    .WithEnvironment("Serilog__WriteTo__2__Name", "Seq")
    .WithEnvironment("Serilog__WriteTo__2__Args__serverUrl", "http://localhost:5341")
    .WithEnvironment("Serilog__WriteTo__2__Args__apiKey", "")

    ;

builder.AddProject<Projects.Messaging_Console_Kafka_Consumer_App>("messaging-console-kafka-consumer-app")
    .WithEnvironment("DOTNET_ENVIRONMENT", "Development")
    .WithEnvironment("KafkaOptions__HostName", "localhost")
    .WithEnvironment("KafkaOptions__Port", "9094")

    //this is more of a demonstration of "this is also possible". 
    .WithEnvironment("Serilog__WriteTo__2__Name", "Seq")
    .WithEnvironment("Serilog__WriteTo__2__Args__serverUrl", "http://localhost:5341")
    .WithEnvironment("Serilog__WriteTo__2__Args__apiKey", "")
    ;

w.WithUrl("http://localhost:8080", "RedPanda");
w.WithUrl("http://localhost:8081", "Seq");

builder.Build().Run();
