using MediatR;
using OIO.Application.Context.UserContext.Hubs;
using OIO.Domain.Context.OrderContext.Aggregates.Orders.Events;

namespace OIO.Application.Context.OrderContext.EventHandlers;

/// <summary>
/// Pushes order status changes to buyer and seller via UserHub.
/// Currently handles OrderCancelledEvent. Additional order lifecycle events
/// can be added here as domain events are introduced for other transitions.
/// </summary>
internal sealed class OrderSignalREventHandlers :
    INotificationHandler<OrderCancelledEvent>
{
    private readonly IUserNotificationService _userNotificationService;

    public OrderSignalREventHandlers(IUserNotificationService userNotificationService)
    {
        _userNotificationService = userNotificationService;
    }

    public async Task Handle(OrderCancelledEvent notification, CancellationToken ct)
    {
        var buyerId = Guid.Parse(notification.BuyerId);

        await _userNotificationService.NotifyOrderStatusChangedAsync(
            buyerId,
            new OrderStatusChangedNotification(
                OrderId: Guid.Parse(notification.OrderId),
                OrderNumber: notification.OrderNumber,
                NewStatus: "cancelled",
                PreviousStatus: ""));
    }
}
