using Messaging.Library;
using Microsoft.AspNetCore.Mvc;
using Wolverine;

namespace Messaging.Kafka.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController(IMessageBus messageBus, ILogger<OrdersController> logger) : ControllerBase
{
    //[HttpPost]
    //public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request)
    //{
    //    logger.LogInformation("Received Create Order Request: {@request}", request);
    //    var orderId = Guid.NewGuid();

    //    await messageBus.PublishAsync(
    //        new OrderCreated(orderId, request.CustomerName, request.Amount),
    //        new DeliveryOptions
    //        {
    //            Headers =
    //            {
    //                ["CreatedBy"] = "WebAPI",
    //                ["Timestamp"] = DateTimeOffset.UtcNow.ToString(),
    //                ["CorrelationId"] = HttpContext.TraceIdentifier
    //            }
    //        });

    //    return Ok(new { OrderId = orderId });
    //}

    //[HttpPost("urgent")]
    //public async Task<IActionResult> CreateUrgentOrder([FromBody] CreateOrderRequest request)
    //{
    //    var orderId = Guid.NewGuid();

    //    await messageBus.PublishAsync(
    //        new UrgentOrderCreated(orderId, request.CustomerName, request.Amount),
    //        new DeliveryOptions
    //        {
    //            Headers =
    //            {
    //                ["Priority"] = "Critical",
    //                ["Timestamp"] = DateTimeOffset.UtcNow.ToString(),
    //                ["RequiresAck"] = "true"
    //            }
    //        });

    //    return Ok(new { OrderId = orderId });
    //}
}