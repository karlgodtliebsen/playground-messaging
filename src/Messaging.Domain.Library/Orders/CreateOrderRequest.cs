namespace Messaging.Domain.Library.Orders;

public record CreateOrderRequest(string CustomerName, decimal Amount);