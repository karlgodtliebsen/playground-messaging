using Messaging.Library.Messages;
using Messaging.Library.Orders;
using Messaging.Library.Payments;
using Messaging.RabbitMq.WebApi.Configuration;

using Scalar.AspNetCore;

using Serilog;
using Serilog.Events;

using Wolverine;

const string title = "Messaging Wolverine RabbitMq WebApi";
Console.Title = title;
Console.WriteLine(title);

var builder = WebApplication.CreateBuilder(args);


//builder.Host.UseWolverine(RabbitMqConfigurator.BuildWolverine());

builder.AddServiceDefaults();

var services = builder.Services;
var configuration = builder.Configuration;
// Add services to the container.
services
    .AddProducerServices(configuration)
    .AddConsumerServices(configuration)
    .AddLogging(loggingBuilder => { services.AddSerilog(loggingBuilder, configuration); });
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
    app.MapScalarApiReference();
    app.MapDefaultEndpoints();
}

//app.UseHttpsRedirection();
//app.UseAuthorization();

app.MapControllers();

//// This creates a POST endpoint that automatically handles the request/response
//app.MapWolverineEndpoints(opts =>
//{
//    // Creates POST /createordercommand endpoint automatically
//    opts.IncludeType<CreateOrderCommand>();

//    // You can customize the route
//    opts.RouteFor<CreateOrderCommand>("orders/create");
//});


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


//app.MapPost("/orders/create-sync", (CreateOrderRequest request, IMessageBus bus) =>
//    bus.InvokeAsync<CreateOrderResponse>(new CreateOrderCommand(request.CustomerName, request.Amount)));

////fire-and-forget
//app.MapPost("/orders/create-ff", async (CreateOrderRequest request, IMessageBus bus) =>
//{
//    var orderId = Guid.NewGuid();
//    await bus.PublishAsync(new OrderCreated(orderId, request.CustomerName, request.Amount));
//    return Results.Ok(new { OrderId = orderId });
//});

//// Fire-and-forget (async processing)
//app.MapPost("/orders/create-async", async (CreateOrderRequest request, IMessageBus bus) =>
//{
//    var orderId = Guid.NewGuid();
//    await bus.PublishAsync(new OrderCreated(orderId, request.CustomerName, request.Amount));
//    return Results.Accepted(value: new { OrderId = orderId, Status = "Processing" });
//});

Console.WriteLine("Use this Url for Scalar {0}", "http://localhost:5179/scalar/v1");
Log.Logger.Information("Starting {title} Web API", title);

app.Run();

