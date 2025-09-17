namespace Messaging.Library.Payments;

public record ProcessPaymentRequest(Guid OrderId, decimal Amount, string PaymentMethod);