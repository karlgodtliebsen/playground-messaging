using Messaging.Application.Configuration;
using Messaging.Application.Configuration.KafkaSupport;

using Microsoft.Extensions.Hosting;

const string title = "Messaging Kafka Producer Demo";

Console.Title = title;
Console.WriteLine(title);
CancellationTokenSource cancellationTokenSource = new();

var kafkaProducerHost = KafkaBuilder.BuildKafkaProducerHost();
var serviceProvider = kafkaProducerHost.Services;
var seriLogger = serviceProvider.SetupSerilog();
seriLogger.Information("Starting Host {title}", title);





await kafkaProducerHost.RunAsync(cancellationTokenSource.Token);
