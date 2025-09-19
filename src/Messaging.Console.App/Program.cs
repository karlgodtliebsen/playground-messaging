
using Messaging.Console.App.Configuration;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

const string title = "Messaging Demo";

Console.Title = title;
Console.WriteLine(title);
CancellationTokenSource cancellationTokenSource = new();

var rabbitMqDiagnosticsConsumerHost = HostConfigurator.BuildRabbitMqDiagnosticsConsumerHost();
var rabbitMqDiagnosticsProducerHost = HostConfigurator.BuildRabbitMqDiagnosticsProducerHost();

//var kafkaProducerHost = HostConfigurator.BuildKafkaProducerHost();
//var kafkaConsumerHost = HostConfigurator.BuildKafkaConsumerHost();
//var rabbitMqProducerHost = HostConfigurator.BuildRabbitMqProducerHost();
//var rabbitMqConsumerHost = HostConfigurator.BuildRabbitMqConsumerHost();

var host = rabbitMqDiagnosticsProducerHost;
var serviceProvider = host.Services;
var logger = serviceProvider.SetupSerilog();
var mLogger = serviceProvider.GetRequiredService<ILogger<Program>>();

logger.Information("Starting Multi Host {title}", title);
//start multiple hosts
await HostConfigurator.RunHostsAsync([
    /*kafkaProducerHost, kafkaConsumerHost, rabbitMqProducerHost, rabbitMqConsumerHost,*/
    rabbitMqDiagnosticsProducerHost,
    rabbitMqDiagnosticsConsumerHost
], title, mLogger, cancellationTokenSource.Token);
