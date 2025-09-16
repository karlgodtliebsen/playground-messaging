// See https://aka.ms/new-console-template for more information

using Messaging.Console.App.Configuration;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

const string title = "Messaging Demo";

Console.Title = title;
Console.WriteLine(title);
CancellationTokenSource cancellationTokenSource = new();

//start a test container instance for postgresql. get the connection string and pass along
//var serilogHost = HostConfigurator.BuildApplicationLoggingHostUsingSqLite();
//var postgreSqlHost = HostConfigurator.BuildApplicationLoggingHostUsingPostgreSql();

var mssqlHost = HostConfigurator.BuildApplicationLoggingHostUsingMsSql();
//var monitorHost = HostConfigurator.BuildMonitorHost();
//var producerHost = HostConfigurator.BuildProducerHost();
//var sLogger = mssqlHost.Services.GetRequiredService<Serilog.ILogger>();
var mLogger = mssqlHost.Services.GetRequiredService<ILogger<Program>>();


////start multiple hosts
await HostConfigurator.RunHostsAsync([mssqlHost/*, monitorHost, producerHost*/], title, mLogger, cancellationTokenSource.Token);


