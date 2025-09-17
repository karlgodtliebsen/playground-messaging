var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.Messaging_WebApi>("messaging-webapi");

builder.Build().Run();
