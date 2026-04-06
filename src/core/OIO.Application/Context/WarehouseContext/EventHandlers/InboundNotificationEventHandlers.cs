using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.NotificationContext;
using OIO.Application.Context.NotificationContext.Commands.CreateNotification;
using OIO.Domain.Context.NotificationContext.Enums;
using OIO.Domain.Context.WarehouseContext.Aggregates.InboundShipments;
using OIO.Domain.Context.WarehouseContext.Aggregates.InboundShipments.Events;
using OIO.Domain.Context.WarehouseContext.ValueObjects.Ids;

namespace OIO.Application.Context.WarehouseContext.EventHandlers;

internal sealed class InboundShipmentPickedUpEventHandler(
    ISender sender,
    IDbContext dbContext,
    ILogger<InboundShipmentPickedUpEventHandler> logger)
    : INotificationHandler<InboundShipmentPickedUpEvent>
{
    public async Task Handle(InboundShipmentPickedUpEvent notification, CancellationToken cancellationToken)
    {
        var shipment = await dbContext.Set<InboundShipment>()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == InboundShipmentId.From(Guid.Parse(notification.InboundShipmentId)), cancellationToken);

        if (shipment is null)
            return;

        await NotificationDispatch.DispatchAsync(
            sender,
            logger,
            new CreateNotificationCommand(
                UserId: shipment.SellerId.Value,
                NotificationType: "warehouse",
                EventType: "inbound_picked_up",
                Title: "Lay hang thanh cong",
                Message: $"Don vi van chuyen da lay goi hang {shipment.ClientOrderCode} tu ban va dang tren duong den kho OIO.",
                Priority: NotificationPriority.Normal,
                EntityType: "InboundShipment",
                EntityId: shipment.Id.Value),
            cancellationToken);
    }
}

internal sealed class InboundShipmentDeliveringEventHandler(
    ISender sender,
    IDbContext dbContext,
    ILogger<InboundShipmentDeliveringEventHandler> logger)
    : INotificationHandler<InboundShipmentDeliveringEvent>
{
    public async Task Handle(InboundShipmentDeliveringEvent notification, CancellationToken cancellationToken)
    {
        var shipment = await dbContext.Set<InboundShipment>()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == InboundShipmentId.From(Guid.Parse(notification.InboundShipmentId)), cancellationToken);

        if (shipment is null)
            return;

        await NotificationDispatch.DispatchAsync(
            sender,
            logger,
            new CreateNotificationCommand(
                UserId: shipment.SellerId.Value,
                NotificationType: "warehouse",
                EventType: "inbound_delivering",
                Title: "Goi hang sap den kho",
                Message: $"Goi hang {shipment.ClientOrderCode} dang duoc shipper giao den kho OIO.",
                Priority: NotificationPriority.Normal,
                EntityType: "InboundShipment",
                EntityId: shipment.Id.Value),
            cancellationToken);
    }
}

internal sealed class InboundShipmentCancelledNotificationHandler(
    ISender sender,
    IDbContext dbContext,
    ILogger<InboundShipmentCancelledNotificationHandler> logger)
    : INotificationHandler<InboundShipmentCancelledEvent>
{
    public async Task Handle(InboundShipmentCancelledEvent notification, CancellationToken cancellationToken)
    {
        var shipment = await dbContext.Set<InboundShipment>()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == InboundShipmentId.From(Guid.Parse(notification.InboundShipmentId)), cancellationToken);

        if (shipment is null)
            return;

        await NotificationDispatch.DispatchAsync(
            sender,
            logger,
            new CreateNotificationCommand(
                UserId: shipment.SellerId.Value,
                NotificationType: "warehouse",
                EventType: "inbound_cancelled",
                Title: "Goi hang da bi huy",
                Message: $"Goi hang {shipment.ClientOrderCode} den kho OIO da bi huy. Ly do: {notification.Reason}",
                Priority: NotificationPriority.High,
                EntityType: "InboundShipment",
                EntityId: shipment.Id.Value),
            cancellationToken);
    }
}

internal sealed class InboundShipmentFailedNotificationHandler(
    ISender sender,
    IDbContext dbContext,
    ILogger<InboundShipmentFailedNotificationHandler> logger)
    : INotificationHandler<InboundShipmentFailedEvent>
{
    public async Task Handle(InboundShipmentFailedEvent notification, CancellationToken cancellationToken)
    {
        var shipment = await dbContext.Set<InboundShipment>()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == InboundShipmentId.From(Guid.Parse(notification.InboundShipmentId)), cancellationToken);

        if (shipment is null)
            return;

        await NotificationDispatch.DispatchAsync(
            sender,
            logger,
            new CreateNotificationCommand(
                UserId: shipment.SellerId.Value,
                NotificationType: "warehouse",
                EventType: "inbound_failed",
                Title: "Giao hang den kho that bai",
                Message: $"Goi hang {shipment.ClientOrderCode} den kho OIO da gap su co. Ly do: {notification.Reason}",
                Priority: NotificationPriority.High,
                EntityType: "InboundShipment",
                EntityId: shipment.Id.Value),
            cancellationToken);
    }
}
