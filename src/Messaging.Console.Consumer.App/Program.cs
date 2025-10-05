using Messaging.Application.Configuration;
using Messaging.Application.Configuration.KafkaSupport;
using Messaging.Hosting.Library;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

const string title = "Messaging Kafka Consumer Demo";

Console.Title = title;
Console.WriteLine(title);
CancellationTokenSource cancellationTokenSource = new();

//var kafkaConsumerHost = KafkaBuilder.BuildKafkaCombinedHost();
//var serviceProvider = kafkaConsumerHost.Services;
//serviceProvider.UseKafkaEventListener();
//var logger = serviceProvider.SetupSerilog();
//logger.Information("Starting Host {title}", title);
//await kafkaConsumerHost.RunAsync(cancellationTokenSource.Token);

var allHosts = new List<IHost>();
for (int i = 0; i < 5; i++)
{
    var kafkaConsumerHost = KafkaBuilder.BuildKafkaConsumerHost();
    kafkaConsumerHost.Services.UseKafkaEventListener();
    allHosts.Add(kafkaConsumerHost);
}

var serviceProvider = allHosts[0].Services;
var logger = serviceProvider.SetupSerilog();
var mLogger = serviceProvider.GetRequiredService<ILogger<Program>>();
logger.Information("Starting Multi Host {title}", title);

//start multiple hosts
await BackgroundServicesRunner.RunAsync(allHosts.ToArray(), title, mLogger, cancellationTokenSource.Token);
