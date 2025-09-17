using Messaging.Library;
using Messaging.WebApi.Configuration;

using Serilog;
using Serilog.Events;

using Wolverine;

const string title = "Messaging WebApi";
Console.Title = title;
Console.WriteLine(title);


var builder = WebApplication.CreateBuilder(args);
builder.Host.UseWolverine();
builder.AddServiceDefaults();
var services = builder.Services;
var configuration = builder.Configuration;
// Add services to the container.
services.AddLogging(loggingBuilder => { services.AddSerilog(loggingBuilder, configuration); });
services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
services.AddOpenApi();

var app = builder.Build();
app.Services.SetupSerilog(LogEventLevel.Verbose);

app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

//app.UseHttpsRedirection();
//app.UseAuthorization();

app.MapControllers();

app.MapPost("/messages/create", (CreateMessage msg, IMessageBus bus) => bus.InvokeAsync(msg));
app.MapPost("/messages/information", (InformationMessage msg, IMessageBus bus) => bus.InvokeAsync(msg));
app.MapPost("/messages/order-places", (OrderPlaced msg, IMessageBus bus) => bus.InvokeAsync(msg));


Console.WriteLine("Use this Url for Scalar {0}", "http://localhost:5179/scalar/v1");
Log.Logger.Information("Starting {title} Web API", title);

app.Run();

