using MediatR;
using Microsoft.Extensions.Logging;

namespace OIO.Application.Context.WarehouseContext.EventHandlers;

// TODO: This event should ideally reside in a Shared/Integration project or OrderContext (e.g. OrderPaidDomainEvent)
//       and be published when the payment gateway marks an Order as Paid.
public sealed record OrderPaidIntegrationEvent(Guid OrderId) : INotification;

internal sealed class OrderPaidIntegrationEventHandler : INotificationHandler<OrderPaidIntegrationEvent>
{
    private readonly ILogger<OrderPaidIntegrationEventHandler> _logger;
    // private readonly ISender _sender;
    // private readonly IDbContext _dbContext;

    public OrderPaidIntegrationEventHandler(ILogger<OrderPaidIntegrationEventHandler> logger)
    {
        _logger = logger;
    }

    public Task Handle(OrderPaidIntegrationEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("OrderPaidIntegrationEvent received for OrderId: {OrderId}", notification.OrderId);

        // TODO: To fully automate Outbound Shipments when an order is paid:
        // 1. Fetch the Order from the database (OrderContext) using notification.OrderId.
        // 2. Fetch the Auction -> Item -> WarehouseItem to map to `BookOutboundShipmentCommand.WarehouseItemId`.
        // 3. Extract Buyer (Recipient) address from Order.Shipping payload.
        // 4. Extract Package dimensions from WarehouseItem.Dimensions.
        // 5. Build and send `BookOutboundShipmentCommand` via ISender.

        /* Example outline:
           var command = new BookOutboundShipmentCommand(
               OrderId: notification.OrderId,
               WarehouseItemId: warehouseItem.Id.Value,
               RecipientName: order.Shipping.FullName,
               RecipientPhone: order.Shipping.Phone,
               ...
           );
           
           await _sender.Send(command, cancellationToken);
        */

        return Task.CompletedTask;
    }
}
