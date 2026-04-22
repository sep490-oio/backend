using MediatR;
using Microsoft.Extensions.Logging;
using OIO.Application.Context.NotificationContext;
using OIO.Application.Context.NotificationContext.Commands.CreateNotification;
using OIO.Domain.Context.NotificationContext.Enums;
using OIO.Domain.Context.WarehouseContext.Aggregates.WarehouseToSellerShipments.Events;

namespace OIO.Application.Context.WarehouseContext.EventHandlers;

/// <summary>
/// Notifies the seller that a rejected warehouse item is being returned to them.
/// Tracking info arrives in a separate notification once warehouse staff ship it
/// (see MarkWarehouseReturnShipped in Phase D).
/// </summary>
internal sealed class WarehouseToSellerShipmentCreatedEventHandler(
    ISender sender,
    ILogger<WarehouseToSellerShipmentCreatedEventHandler> logger)
    : INotificationHandler<WarehouseToSellerShipmentCreatedEvent>
{
    public async Task Handle(WarehouseToSellerShipmentCreatedEvent notification, CancellationToken cancellationToken)
    {
        await NotificationDispatch.DispatchAsync(
            sender,
            logger,
            new CreateNotificationCommand(
                UserId:           notification.SellerId,
                NotificationType: "warehouse",
                EventType:        "warehouse_return_to_seller_created",
                Title:            "Kho se tra lai hang cho ban",
                Message:          $"Mot mon hang bi tu choi se duoc gui tra lai cho ban tu kho. Ly do: {notification.RejectionReason}. Thong tin van chuyen se duoc cap nhat khi nhan vien kho gui hang.",
                Priority:         NotificationPriority.High,
                EntityType:       "WarehouseToSellerShipment",
                EntityId:         notification.WarehouseToSellerShipmentId),
            cancellationToken);
    }
}
