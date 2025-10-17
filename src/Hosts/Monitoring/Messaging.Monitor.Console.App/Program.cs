
// Program.cs

using Messaging.Monitor.Console.App;

using RazorConsole.Core;

//var meterFactory = serviceProvider.GetRequiredService<IMeterFactory>();
//var activityFactory = serviceProvider.GetRequiredService<IActivitySourceFactory>();
//var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
//logger.LogWarning("=== METER DIAGNOSTIC ===");
//logger.LogWarning("ActivitySourceFactory Meter Name: {MeterName}", activityFactory.Meter.Name);

//var eventHub = serviceProvider.GetService<EventHubMetrics>();
//if (eventHub != null)
//{
//    logger.LogWarning("EventHub metrics is registered");
//}
//else
//{
//    logger.LogWarning("EventHub metrics is NOT registered");
//}
await AppHost.RunAsync<Counter>();