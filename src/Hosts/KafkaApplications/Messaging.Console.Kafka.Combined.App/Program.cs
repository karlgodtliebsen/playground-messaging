using Messaging.Application.Configuration;
using Messaging.Application.Configuration.KafkaSupport;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

const string title = "Kafka Messaging Demo";

Console.Title = title;
Console.WriteLine(title);
CancellationTokenSource cancellationTokenSource = new();

//var kafkaProducerHost = KafkaBuilder.BuildKafkaProducerHost();
//var host = kafkaProducerHost;
//var kafkaConsumerHost = KafkaBuilder.BuildKafkaConsumerHost();
//var host = kafkaConsumerHost;

var kafkaCombinedHost = KafkaBuilder.BuildKafkaCombinedHost();
var host = kafkaCombinedHost;

var serviceProvider = host.Services;

serviceProvider.UseKafkaEventListener();

var logger = serviceProvider.SetupSerilog();
var mLogger = serviceProvider.GetRequiredService<ILogger<Program>>();
logger.Information("Starting Kafka Host {title}", title);

await kafkaCombinedHost.RunAsync(cancellationTokenSource.Token);


//start multiple hosts
//await BackgroundServicesRunner.RunAsync([
//   // kafkaProducerHost,
//   //  kafkaConsumerHost, 
//   kafkaCombinedHost
//], title, mLogger, cancellationTokenSource.Token);

