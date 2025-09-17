namespace Messaging.Library.Payments;

public record PaymentProcessed(Guid OrderId, decimal Amount, string PaymentMethod);

