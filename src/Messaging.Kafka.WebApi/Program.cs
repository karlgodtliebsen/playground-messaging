using Messaging.Domain.Library.Orders;
using Messaging.Domain.Library.Payments;
using Messaging.Domain.Library.SimpleMessages;
using Messaging.Kafka.Library.Configuration;
using Messaging.Kafka.WebApi.Configuration;

using Scalar.AspNetCore;

using Serilog;
using Serilog.Events;

using Wolverine;

const string title = "Messaging Wolverine Kafka WebApi";
Console.Title = title;
Console.WriteLine(title);

var builder = WebApplication.CreateBuilder(args);

//var kafkaConfig = builder.Configuration.GetSection("Kafka").Get<KafkaConfig>();
//var kafkaConfig = builder.Configuration.GetSection("Kafka");
builder.Host.UseWolverine(KafkaConfigurator.Build);


builder.AddServiceDefaults();

var services = builder.Services;
var configuration = builder.Configuration;

services.AddLogging(loggingBuilder => { services.AddSerilog(loggingBuilder, configuration); });
services.AddControllers();
services.AddOpenApi();

var app = builder.Build();
app.Services.SetupSerilog(LogEventLevel.Verbose);

app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
    app.MapDefaultEndpoints();
}

//app.UseHttpsRedirection();
//app.UseAuthorization();

app.MapControllers();

app.MapPost("/messages/create", (CreateMessage msg, IMessageBus bus) => bus.InvokeAsync(msg));
app.MapPost("/messages/information", (InformationMessage msg, IMessageBus bus) => bus.InvokeAsync(msg));


app.MapPost("/orders", async (CreateOrderRequest request, IMessageBus messageBus) =>
{
    var orderId = Guid.NewGuid();

    // Publish with custom headers
    await messageBus.PublishAsync(
        new OrderCreated(orderId, request.CustomerName, request.Amount, DateTimeOffset.UtcNow),
        new DeliveryOptions
        {
            Headers =
            {
                ["Source"] = "WebAPI",
                ["CorrelationId"] = Guid.NewGuid().ToString(),
                ["Timestamp"] = DateTimeOffset.UtcNow.ToString("O"),
                ["Priority"] = request.Amount > 1000 ? "High" : "Normal"
            }
        });

    return Results.Ok(new { OrderId = orderId });
});


app.MapPut("/orders/{orderId}/status", async (Guid orderId, UpdateOrderStatusRequest request, IMessageBus messageBus) =>
{
    await messageBus.PublishAsync(
        new OrderUpdated(orderId, request.Status, DateTimeOffset.UtcNow));

    return Results.Ok();
});

app.MapPost("/payments", async (ProcessPaymentRequest request, IMessageBus messageBus) =>
{
    await messageBus.PublishAsync(
        new PaymentProcessed(request.OrderId, request.Amount, request.PaymentMethod),
        new DeliveryOptions
        {
            Headers =
            {
                ["PaymentProvider"] = request.PaymentMethod,
                ["ProcessedAt"] = DateTimeOffset.UtcNow.ToString("O")
            }
        });

    return Results.Ok();
});


Console.WriteLine("Use this Url for Scalar {0}", "http://localhost:5178/scalar/v1");
Log.Logger.Information("Starting {title} Web API", title);

app.Run();

