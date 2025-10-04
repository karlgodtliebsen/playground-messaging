
using Messaging.Console.App.Configuration;
using Messaging.Console.App.Configuration.KafkaSupport;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

const string title = "Messaging Demo";

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
logger.Information("Starting Multi Host {title}", title);

await kafkaCombinedHost.RunAsync(cancellationTokenSource.Token);


//start multiple hosts

//await BackgroundServicesRunner.RunAsync([
//   // kafkaProducerHost,
//   //  kafkaConsumerHost, 
//   kafkaCombinedHost
//], title, mLogger, cancellationTokenSource.Token);

//rabbitMqProducerHost,
//rabbitMqConsumerHost

//var rabbitMqConsumerHost = RabbitMqBuilder.BuildRabbitMqConsumerHost();
//var host = rabbitMqConsumerHost;
//var rabbitMqProducerHost = RabbitMqBuilder.BuildRabbitMqProducerHost();
//var host = rabbitMqProducerHost;
//var host = HostBuilder.BuildRabbitMqCombinedHost();