using Microsoft.Extensions.Logging;
using Wolverine;

namespace Messaging.Domain.Library.Orders.Handlers;

public class OrderCommandHandler(IMessageBus messageBus, ILogger<OrderCommandHandler> logger)
{
    // This handles the command and returns a response
    public async Task<CreateOrderResponse> Handle(CreateOrderCommand command)
    {
        logger.LogInformation("OrderCommandHandler  Received Create Order Command order {amount} for {customerName}", command.Amount, command.CustomerName);
        var orderId = Guid.NewGuid();

        // Create the response first
        var response = new CreateOrderResponse(orderId, "Created");

        // Then publish the event for other handlers to process
        await messageBus.PublishAsync(new OrderCreated(orderId, command.CustomerName, command.Amount, DateTimeOffset.UtcNow));

        return response;
    }
}