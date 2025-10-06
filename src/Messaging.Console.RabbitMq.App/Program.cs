using Messaging.Application.Configuration;
using Messaging.Application.Configuration.RabbitMqSupport;
using Messaging.Hosting.Library;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

const string title = "RabbitMq Messaging Demo";

Console.Title = title;
Console.WriteLine(title);
CancellationTokenSource cancellationTokenSource = new();

//var rabbitMqConsumerHost = RabbitMqBuilder.BuildRabbitMqConsumerHost();
//var host = rabbitMqConsumerHost;
var rabbitMqProducerHost = RabbitMqBuilder.BuildRabbitMqProducerHost();
var host = rabbitMqProducerHost;

//var host = HostBuilder.BuildRabbitMqCombinedHost();

var serviceProvider = host.Services;

var logger = serviceProvider.SetupSerilog();
var mLogger = serviceProvider.GetRequiredService<ILogger<Program>>();
logger.Information("Starting RabbitMq Host {title}", title);

//start multiple hosts

await BackgroundServicesRunner.RunAsync([
   // rabbitMqConsumerHost,
     rabbitMqProducerHost,
], title, mLogger, cancellationTokenSource.Token);