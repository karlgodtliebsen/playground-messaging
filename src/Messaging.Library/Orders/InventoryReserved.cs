namespace Messaging.Library.Orders;

public record InventoryReserved(Guid OrderId, string ProductId, int Quantity);