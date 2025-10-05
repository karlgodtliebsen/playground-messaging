namespace Messaging.Domain.Library.Orders;

public record CreateOrderCommand(string CustomerName, decimal Amount);