namespace Messaging.Library.Orders;

public record OrderCreated(Guid OrderId, string CustomerName, decimal Amount, DateTimeOffset CreatedAt);

