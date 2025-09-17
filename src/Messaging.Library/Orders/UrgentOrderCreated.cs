using Wolverine.Attributes;

namespace Messaging.Library.Orders;

[Topic("orders.priority.high")]
public record UrgentOrderCreated(Guid OrderId, string CustomerName, decimal Amount);