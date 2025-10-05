namespace Messaging.Domain.Library.Payments;

public record ProcessPaymentRequest(Guid OrderId, decimal Amount, string PaymentMethod);