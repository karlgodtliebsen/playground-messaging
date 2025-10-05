namespace Messaging.Domain.Library.Orders;

public record CreateOrderResponse(Guid OrderId, string Status);