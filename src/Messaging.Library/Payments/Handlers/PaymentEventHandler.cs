using Messaging.Library.Orders;
using Microsoft.Extensions.Logging;
using Wolverine;

namespace Messaging.Library.Payments.Handlers;

public class PaymentEventHandler(IMessageBus messageBus, ILogger<PaymentEventHandler> logger)
{
    // Handler with envelope to access Kafka metadata
    public void Handle(PaymentProcessed payment, Envelope envelope)
    {
        logger.LogInformation("PaymentEventHandler Payment processed for order {OrderId}", payment.OrderId);

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