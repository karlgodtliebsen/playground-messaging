
using Messaging.Console.App.Configuration;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

const string title = "Messaging Demo";

Console.Title = title;
Console.WriteLine(title);
CancellationTokenSource cancellationTokenSource = new();
//var kafkaProducerHost = HostBuilder.BuildKafkaProducerHost();
//var kafkaConsumerHost = HostBuilder.BuildKafkaConsumerHost();

//var rabbitMqConsumerHost = HostBuilder.BuildRabbitMqConsumerHost();
//var host = rabbitMqConsumerHost;
//var rabbitMqProducerHost = HostBuilder.BuildRabbitMqProducerHost();
//var host = rabbitMqProducerHost;

var host = HostBuilder.BuildRabbitMqCombinedHost();

var serviceProvider = host.Services;
var logger = serviceProvider.SetupSerilog();
var mLogger = serviceProvider.GetRequiredService<ILogger<Program>>();

logger.Information("Starting Multi Host {title}", title);
//start multiple hosts
await HostBuilder.RunHostsAsync([
    /*kafkaProducerHost,
     kafkaConsumerHost, */
    //rabbitMqProducerHost,
    //rabbitMqConsumerHost
    host
], title, mLogger, cancellationTokenSource.Token);
