namespace Messaging.Domain.Library.Payments;

public record PaymentProcessed(Guid OrderId, decimal Amount, string PaymentMethod);

