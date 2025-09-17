namespace Messaging.Library.Orders;

public record CreateOrderCommand(string CustomerName, decimal Amount);