
using Messaging.Console.App.Configuration;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

const string title = "Messaging Demo";

Console.Title = title;
Console.WriteLine(title);
CancellationTokenSource cancellationTokenSource = new();
//var kafkaProducerHost = HostConfigurator.BuildKafkaProducerHost();
//var kafkaConsumerHost = HostConfigurator.BuildKafkaConsumerHost();

//var rabbitMqConsumerHost = HostConfigurator.BuildRabbitMqConsumerHost();
//var rabbitMqProducerHost = HostConfigurator.BuildRabbitMqProducerHost();
//var host = rabbitMqProducerHost;

var host = HostConfigurator.BuildRabbitMqCombinedHost();

var serviceProvider = host.Services;
var logger = serviceProvider.SetupSerilog();
var mLogger = serviceProvider.GetRequiredService<ILogger<Program>>();

logger.Information("Starting Multi Host {title}", title);
//start multiple hosts
await HostConfigurator.RunHostsAsync([
    /*kafkaProducerHost,
     kafkaConsumerHost, */
    //rabbitMqProducerHost,
    //rabbitMqConsumerHost
    host
], title, mLogger, cancellationTokenSource.Token);
