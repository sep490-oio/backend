using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OIO.Application.Abstractions.Data;
using OIO.Application.Context.NotificationContext;
using OIO.Application.Context.NotificationContext.Commands.CreateNotification;
using OIO.Domain.Context.NotificationContext.Enums;
using OIO.Domain.Context.OrderContext.Aggregates.Orders;
using OIO.Domain.Context.OrderContext.Aggregates.Orders.Events;
using OIO.Domain.Context.OrderContext.ValueObjects.Ids;

namespace OIO.Application.Context.OrderContext.EventHandlers;

/// <summary>
/// Notifies the buyer after a dispute resolution opens an <see cref="OrderReturn"/>
/// on their behalf. Mirrors the existing order-lifecycle notification handler
/// pattern using <see cref="NotificationDispatch"/> (SignalR + Email routing is
/// handled downstream by the notification command handler per user prefs).
/// </summary>
internal sealed class OrderReturnOpenedByDisputeEventHandler(
    ISender sender,
    IDbContext dbContext,
    ILogger<OrderReturnOpenedByDisputeEventHandler> logger)
    : INotificationHandler<OrderReturnOpenedByDisputeEvent>
{
    public async Task Handle(OrderReturnOpenedByDisputeEvent notification, CancellationToken cancellationToken)
    {
        var order = await dbContext.Set<Order>()
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == OrderId.From(notification.OrderId), cancellationToken);

        if (order is null)
        {
            logger.LogWarning(
                "OrderReturnOpenedByDisputeEventHandler: Order {OrderId} not found.",
                notification.OrderId);
            return;
        }

        await NotificationDispatch.DispatchAsync(
            sender,
            logger,
            new CreateNotificationCommand(
                UserId:           notification.BuyerId,
                NotificationType: "order",
                EventType:        "order_return_opened_by_dispute",
                Title:            "Yeu cau tra hang da duoc mo",
                Message:          $"Khieu nai cua ban tren don hang {order.OrderNumber.Value} da duoc giai quyet voi ket qua tra hang. Vui long gui hang lai truoc {notification.BuyerDecisionDueAt:yyyy-MM-dd HH:mm} UTC.",
                Priority:         NotificationPriority.High,
                EntityType:       "OrderReturn",
                EntityId:         notification.OrderReturnId),
            cancellationToken);
    }
}
