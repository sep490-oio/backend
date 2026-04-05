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

internal sealed class InboundShipmentInspectedEventHandler(
    ISender sender,
    IDbContext dbContext,
    ILogger<InboundShipmentInspectedEventHandler> logger)
    : INotificationHandler<InboundShipmentInspectedEvent>
{
    public async Task Handle(InboundShipmentInspectedEvent notification, CancellationToken cancellationToken)
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
                EventType: "inbound_inspected",
                Title: "Kiem dinh hoan tat",
                Message: $"Goi hang {shipment.ClientOrderCode} cua ban da duoc kiem dinh xong.",
                Priority: NotificationPriority.Normal,
                EntityType: "InboundShipment",
                EntityId: shipment.Id.Value),
            cancellationToken);
    }
}

internal sealed class InboundShipmentReceivedEventHandler(
    ISender sender,
    IDbContext dbContext,
    ILogger<InboundShipmentReceivedEventHandler> logger)
    : INotificationHandler<InboundShipmentReceivedEvent>
{
    public async Task Handle(InboundShipmentReceivedEvent notification, CancellationToken cancellationToken)
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
                EventType: "item_received",
                Title: "Goi hang da duoc tiep nhan",
                Message: $"Mon hang cua ban da duoc nhan vien OIO tiep nhan tai cua kho va dang chuan bi dua len ke.",
                Priority: NotificationPriority.Normal,
                EntityType: "InboundShipment",
                EntityId: shipment.Id.Value),
            cancellationToken);
    }
}
