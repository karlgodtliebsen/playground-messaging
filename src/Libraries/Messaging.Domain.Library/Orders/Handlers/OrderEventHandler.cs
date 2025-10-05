using Microsoft.Extensions.Logging;

namespace Messaging.Domain.Library.Orders.Handlers;

public class OrderEventHandler(ILogger<OrderEventHandler> logger)
{
    public void Handle(OrderCreated order)
    {
        logger.LogInformation("OrderEventHandler Processing OrderCreated {orderId} for {customerName}", order.OrderId, order.CustomerName);
    }

    public void Handle(UrgentOrderCreated order)
    {
        logger.LogInformation("OrderEventHandler Processing UrgentOrderCreated {orderId} for {customerName}", order.OrderId, order.CustomerName);
    }


    public void Handle(OrderUpdated order)
    {
        logger.LogInformation("Order {OrderId} status updated to: {Status}", order.OrderId, order.Status);
    }

    public void Handle(OrderPlaced order)
    {
        logger.LogInformation("OrderEventHandler Processing OrderPlaced {OrderId} with amount: {amount}", order.OrderId, order.Amount);
    }
}