using Messaging.Library.Orders;
using Microsoft.Extensions.Logging;
using Wolverine;

namespace Messaging.Library.Payments.Handlers;

public class PaymentHandler(IMessageBus messageBus, ILogger<PaymentHandler> logger)
{
    // Handler with envelope to access Kafka metadata
    public void Handle(ProcessPaymentRequest payment, Envelope envelope)
    {
        logger.LogInformation("ProcessPaymentRequest Payment processed for order {OrderId}", payment.OrderId);

        // Access Kafka-specific metadata
        if (envelope.Headers.TryGetValue("kafka.partition", out var partition))
        {
            logger.LogInformation("Message received from partition: {Partition}", partition);
        }

        if (envelope.Headers.TryGetValue("kafka.offset", out var offset))
        {
            logger.LogInformation("Message offset: {Offset}", offset);
        }
    }
}