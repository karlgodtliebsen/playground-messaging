# playground-messaging RabbitMq


### Docker commands to run RabbitMq with Management UI

```shell
docker pull rabbitmq
docker pull rabbitmq:3-management

docker run -d --hostname RabbitMq --name rabbitmq-5673 -p 5673:5672 -p 15673:15672 rabbitmq:3-management


-e RABBITMQ_DEFAULT_USER=guest -e RABBITMQ_DEFAULT_PASS=guest 

docker start rabbitmq-5673

add this if relevant:

-v /temp/rabbitmq/rabbitmq-data:/var/lib/rabbitmq 

After it’s up, you can point your browser to http://localhost:15673 

docker stop rabbitmq-5673
docker stop rabbitmq:3-management

(login is guest/guest by default)
```


### Look into:




```csharp

// ===========================================
// Scenario 1: Priority Queues
// ===========================================

public record HighPriorityTask(string TaskId, string Description);
public record LowPriorityTask(string TaskId, string Description);

// In Program.cs
opts.ListenToRabbitQueue("priority-tasks", queue =>
{
    queue.BindToExchange("tasks-exchange", "task.*");
    queue.Durable();
    queue.Arguments.Add("x-max-priority", 10); // Enable priority queue
});

opts.PublishMessage<HighPriorityTask>()
    .ToRabbitQueue("priority-tasks")
    .WithHeader("Priority", 9); // High priority

opts.PublishMessage<LowPriorityTask>()
    .ToRabbitQueue("priority-tasks") 
    .WithHeader("Priority", 1); // Low priority

// ===========================================
// Scenario 2: Delayed Messages (Scheduled)
// ===========================================

public record ScheduledNotification(string UserId, string Message, DateTimeOffset SendAt);

// Handler with scheduling
public class NotificationHandler
{
    public void Handle(ScheduledNotification notification)
    {
        Console.WriteLine($"Sending notification to {notification.UserId}: {notification.Message}");
    }
}

// In controller
[HttpPost("schedule-notification")]
public async Task<IActionResult> ScheduleNotification([FromBody] ScheduleRequest request)
{
    await _messageBus.ScheduleAsync(
        new ScheduledNotification(request.UserId, request.Message, request.SendAt),
        request.SendAt);
    
    return Ok();
}

public record ScheduleRequest(string UserId, string Message, DateTimeOffset SendAt);

// ===========================================
// Scenario 3: Request/Response Pattern
// ===========================================

public record GetOrderStatus(Guid OrderId);
public record OrderStatusResponse(Guid OrderId, string Status, DateTimeOffset LastUpdated);

public class OrderQueryHandler
{
    public OrderStatusResponse Handle(GetOrderStatus query)
    {
        // Simulate database lookup
        return new OrderStatusResponse(query.OrderId, "Processing", DateTimeOffset.UtcNow);
    }
}

// In controller - Request/Response
[HttpGet("{orderId}/status")]
public async Task<IActionResult> GetOrderStatus(Guid orderId)
{
    var response = await _messageBus.InvokeAsync<OrderStatusResponse>(
        new GetOrderStatus(orderId),
        timeout: TimeSpan.FromSeconds(10));
    
    return Ok(response);
}

// ===========================================
// Scenario 4: Message Headers and Metadata
// ===========================================

public class HeaderAwareHandler
{
    public void Handle(OrderCreated order, Envelope envelope)
    {
        // Access message metadata
        var messageId = envelope.Id;
        var timestamp = envelope.Timestamp;
        var source = envelope.Source;
        
        // Access custom headers
        if (envelope.Headers.TryGetValue("CorrelationId", out var correlationId))
        {
            Console.WriteLine($"Processing order with correlation ID: {correlationId}");
        }
        
        if (envelope.Headers.TryGetValue("Priority", out var priority))
        {
            Console.WriteLine($"Message priority: {priority}");
        }
    }
}

// ===========================================
// Scenario 5: Multiple Exchanges Configuration
// ===========================================

// In Program.cs - Complex routing setup
opts.UseRabbitMq(rabbit =>
{
    rabbit.HostName = "localhost";
    
    // Declare multiple exchanges
    rabbit.DeclareExchange("orders-topic", ExchangeType.Topic, isDurable: true);
    rabbit.DeclareExchange("payments-direct", ExchangeType.Direct, isDurable: true);
    rabbit.DeclareExchange("notifications-fanout", ExchangeType.Fanout, isDurable: true);
});

// Route different message types to different exchanges
opts.PublishMessage<OrderCreated>().ToRabbitExchange("orders-topic", "orders.created");
opts.PublishMessage<PaymentProcessed>().ToRabbitExchange("payments-direct", "payment.success");
opts.PublishAllMessages().ToRabbitExchange("notifications-fanout"); // Broadcast all

// ===========================================
// Scenario 6: Dead Letter Queue with Retry Logic
// ===========================================

public class ReliableOrderHandler
{
    public void Handle(OrderCreated order)
    {
        // Simulate processing that might fail
        if (order.Amount > 1000 && Random.Shared.Next(1, 10) <= 3)
        {
            throw new InvalidOperationException("Payment gateway timeout");
        }
        
        Console.WriteLine($"Successfully processed order {order.OrderId}");
    }
}

// Configure dead letter queue in Program.cs
opts.ListenToRabbitQueue("orders-main", queue =>
{
    queue.BindToExchange("orders-exchange", "orders.*");
    queue.Durable();
    // Configure dead letter exchange
    queue.Arguments.Add("x-dead-letter-exchange", "orders-dlx");
    queue.Arguments.Add("x-dead-letter-routing-key", "failed");
    queue.Arguments.Add("x-message-ttl", 30000); // 30 seconds before DLQ
});

opts.ListenToRabbitQueue("orders-dead-letter", queue =>
{
    queue.BindToExchange("orders-dlx", "failed");
    queue.Durable();
});

// Configure retry policy
opts.Policies.OnException<InvalidOperationException>()
    .RetryWithCooldown(maxAttempts: 3, 
                      cooldown: TimeSpan.FromSeconds(2), 
                      maxCooldown: TimeSpan.FromSeconds(10));

// ===========================================
// Scenario 7: Batch Processing
// ===========================================

[MessageBatch(batchSize: 10, timeoutInMilliseconds: 5000)]
public class BatchOrderHandler
{
    public void Handle(OrderCreated[] orders)
    {
        Console.WriteLine($"Processing batch of {orders.Length} orders");
        
        foreach (var order in orders)
        {
            // Process each order in the batch
            Console.WriteLine($"Batch processing order {order.OrderId}");
        }
    }
}

// ===========================================
// Scenario 8: Publisher Confirms
// ===========================================

// In controller - with confirmation
[HttpPost("confirmed")]
public async Task<IActionResult> CreateOrderWithConfirmation([FromBody] CreateOrderRequest request)
{
    var orderId = Guid.NewGuid();
    
    try
    {
        await _messageBus.PublishAsync(
            new OrderCreated(orderId, request.CustomerName, request.Amount),
            new DeliveryOptions
            {
                // Enable publisher confirmation
                DeliverWithin = TimeSpan.FromSeconds(30)
            });
        
        return Ok(new { OrderId = orderId, Status = "Confirmed" });
    }
    catch (TimeoutException)
    {
        return StatusCode(500, "Message delivery could not be confirmed");
    }
}

```





// ===========================================
// Solution 1: Use PublishAsync instead of InvokeAsync
// ===========================================

// This is for fire-and-forget messaging (no response expected)
app.MapPost("/orders/create", async (CreateOrderRequest request, IMessageBus bus) =>
{
    var orderId = Guid.NewGuid();
    var orderCreated = new OrderCreated(orderId, request.CustomerName, request.Amount);
    
    await bus.PublishAsync(orderCreated);
    
    return Results.Ok(new { OrderId = orderId });
});

// ===========================================
// Solution 2: Create a Request/Response Handler
// ===========================================

// Create a command that expects a response
public record CreateOrderCommand(string CustomerName, decimal Amount);
public record CreateOrderResponse(Guid OrderId, string Status);

// Handler that returns a response
public class OrderCommandHandler
{
    public CreateOrderResponse Handle(CreateOrderCommand command)
    {
        var orderId = Guid.NewGuid();
        
        // Process the order creation logic here
        Console.WriteLine($"Creating order for {command.CustomerName}, Amount: {command.Amount}");
        
        // You can also publish events here if needed
        // await _messageBus.PublishAsync(new OrderCreated(orderId, command.CustomerName, command.Amount));
        
        return new CreateOrderResponse(orderId, "Created");
    }
}

// Now this endpoint will work with InvokeAsync
app.MapPost("/orders/create-sync", (CreateOrderRequest request, IMessageBus bus) => 
    bus.InvokeAsync<CreateOrderResponse>(new CreateOrderCommand(request.CustomerName, request.Amount)));

// ===========================================
// Solution 3: Hybrid Approach - Command + Event
// ===========================================

public class HybridOrderHandler
{
    private readonly IMessageBus _messageBus;
    
    public HybridOrderHandler(IMessageBus messageBus)
    {
        _messageBus = messageBus;
    }
    
    // This handles the command and returns a response
    public async Task<CreateOrderResponse> Handle(CreateOrderCommand command)
    {
        var orderId = Guid.NewGuid();
        
        // Create the response first
        var response = new CreateOrderResponse(orderId, "Created");
        
        // Then publish the event for other handlers to process
        await _messageBus.PublishAsync(new OrderCreated(orderId, command.CustomerName, command.Amount));
        
        return response;
    }
}

// Keep your existing event handler for side effects
public class OrderEventHandler
{
    public void Handle(OrderCreated order)
    {
        Console.WriteLine($"Processing order event {order.OrderId} for {order.CustomerName}");
        // Handle logging, notifications, etc.
    }
}

// ===========================================
// Solution 4: Different Endpoints for Different Patterns
// ===========================================

// Fire-and-forget (async processing)
app.MapPost("/orders/create-async", async (CreateOrderRequest request, IMessageBus bus) =>
{
    var orderId = Guid.NewGuid();
    await bus.PublishAsync(new OrderCreated(orderId, request.CustomerName, request.Amount));
    return Results.Accepted(value: new { OrderId = orderId, Status = "Processing" });
});

// Synchronous processing (wait for response)
app.MapPost("/orders/create-sync", (CreateOrderRequest request, IMessageBus bus) =>
    bus.InvokeAsync<CreateOrderResponse>(new CreateOrderCommand(request.CustomerName, request.Amount)));

// ===========================================
// Solution 5: Using Wolverine's Built-in Endpoints
// ===========================================

// Wolverine can automatically create endpoints for your messages
// Add this in Program.cs after building the app

// This creates a POST endpoint that automatically handles the request/response
app.MapWolverineEndpoints(opts =>
{
    // Creates POST /createordercommand endpoint automatically
    opts.IncludeType<CreateOrderCommand>();
    
    // You can customize the route
    opts.RouteFor<CreateOrderCommand>("orders/create");
});

// ===========================================
// Solution 6: Manual Endpoint with Try-Catch
// ===========================================

app.MapPost("/orders/create-safe", async (CreateOrderRequest request, IMessageBus bus) =>
{
    try
    {
        var orderId = Guid.NewGuid();
        
        // Use PublishAsync for fire-and-forget
        await bus.PublishAsync(new OrderCreated(orderId, request.CustomerName, request.Amount));
        
        return Results.Ok(new { 
            OrderId = orderId, 
            Status = "Submitted",
            Message = "Order has been submitted for processing" 
        });
    }
    catch (Exception ex)
    {
        return Results.Problem($"Failed to create order: {ex.Message}");
    }
});

// ===========================================
// Solution 7: Using Wolverine's Execute Pattern
// ===========================================

// For when you want to execute inline without messaging
app.MapPost("/orders/execute", (CreateOrderCommand command, IWolverineRuntime runtime) =>
    runtime.ExecuteAsync(command));

// ===========================================
// Complete Working Example
// ===========================================

// Messages
public record CreateOrderRequest(string CustomerName, decimal Amount);
public record CreateOrderCommand(string CustomerName, decimal Amount);  
public record CreateOrderResponse(Guid OrderId, string Status);
public record OrderCreated(Guid OrderId, string CustomerName, decimal Amount);

// Handlers
public class OrderHandlers
{
    private readonly IMessageBus _messageBus;
    
    public OrderHandlers(IMessageBus messageBus)
    {
        _messageBus = messageBus;
    }
    
    // Command handler (returns response)
    public async Task<CreateOrderResponse> Handle(CreateOrderCommand command)
    {
        var orderId = Guid.NewGuid();
        
        // Publish event for other systems
        await _messageBus.PublishAsync(new OrderCreated(orderId, command.CustomerName, command.Amount));
        
        return new CreateOrderResponse(orderId, "Created");
    }
    
    // Event handler (fire and forget)
    public void Handle(OrderCreated order)
    {
        Console.WriteLine($"Order {order.OrderId} created for {order.CustomerName}");
        // Handle side effects like sending emails, updating inventory, etc.
    }
}

// Endpoints that work
app.MapPost("/orders/async", async (CreateOrderRequest request, IMessageBus bus) =>
{
    var orderId = Guid.NewGuid();
    await bus.PublishAsync(new OrderCreated(orderId, request.CustomerName, request.Amount));
    return Results.Ok(new { OrderId = orderId });
});

app.MapPost("/orders/sync", (CreateOrderRequest request, IMessageBus bus) =>
    bus.InvokeAsync<CreateOrderResponse>(new CreateOrderCommand(request.CustomerName, request.Amount)));

