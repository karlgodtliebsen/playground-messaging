using Messaging.Application.Configuration;
using Messaging.Application.Configuration.KafkaSupport;

using Microsoft.Extensions.Hosting;

const string title = "Messaging Kafka Producer Demo";

Console.Title = title;
Console.WriteLine(title);
CancellationTokenSource cancellationTokenSource = new();

var kafkaProducerHost = KafkaBuilder.BuildKafkaProducerHost();
var serviceProvider = kafkaProducerHost.Services;
var logger = serviceProvider.SetupSerilog();
logger.Information("Starting Host {title}", title);
await kafkaProducerHost.RunAsync(cancellationTokenSource.Token);

