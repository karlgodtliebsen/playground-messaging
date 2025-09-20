namespace Messaging.Domain.Library.Orders;

public record OrderUpdated(Guid OrderId, string Status, DateTimeOffset UpdatedAt);