namespace Messaging.Domain.Library.Orders;

public record OrderPlaced(int OrderId, string Customer, decimal Amount);


//public OrderPlaced WithAmount(decimal newAmount) =>
//    this with { Amount = newAmount };
