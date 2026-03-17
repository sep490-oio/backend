using MediatR;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Commons;
using OIO.Application.Abstractions.Data;
using OIO.Domain.Context.OrderContext.Aggregates.Orders;
using OIO.Domain.Context.OrderContext.ValueObjects.Ids;
using OIO.Domain.Context.WarehouseContext.Aggregates.OutboundShipments.Events;

namespace OIO.Application.Context.OrderContext.EventHandlers;

internal sealed class OrderMarkedShippedEventHandler(
    IDbContext dbContext,
    IUnitOfWork unitOfWork,
    IClock clock)
    : INotificationHandler<OutboundShipmentPickedUpEvent>
{
    public async Task Handle(OutboundShipmentPickedUpEvent notification, CancellationToken cancellationToken)
    {
        var order = await dbContext.Set<Order>()
            .FirstOrDefaultAsync(x => x.Id == OrderId.From(Guid.Parse(notification.OrderId)), cancellationToken);

        if (order is null)
            return;

        var result = order.MarkAsShipped(clock.UtcNow);
        if (result.IsFailure)
            return;

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}

internal sealed class OrderMarkedDeliveredEventHandler(
    IDbContext dbContext,
    IUnitOfWork unitOfWork,
    IClock clock,
    IRuntimeSettings runtimeSettings)
    : INotificationHandler<OutboundShipmentDeliveredEvent>
{
    public async Task Handle(OutboundShipmentDeliveredEvent notification, CancellationToken cancellationToken)
    {
        var order = await dbContext.Set<Order>()
            .FirstOrDefaultAsync(x => x.Id == OrderId.From(Guid.Parse(notification.OrderId)), cancellationToken);

        if (order is null)
            return;

        var decisionWindowDays = runtimeSettings.Order.ReturnDecisionWindowDays;

        var result = order.MarkAsDelivered(
            notification.DeliveredAt,
            notification.DeliveredAt.AddDays(decisionWindowDays),
            clock.UtcNow);

        if (result.IsFailure)
            return;

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
