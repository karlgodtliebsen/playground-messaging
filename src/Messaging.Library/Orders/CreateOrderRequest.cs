namespace Messaging.Library.Orders;

public record CreateOrderRequest(string CustomerName, decimal Amount);