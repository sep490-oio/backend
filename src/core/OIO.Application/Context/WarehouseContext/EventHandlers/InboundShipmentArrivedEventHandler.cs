using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OIO.Application.Abstractions.Data;
using OIO.Application.Context.NotificationContext;
using OIO.Application.Context.NotificationContext.Commands.CreateNotification;
using OIO.Domain.AppDefinitions;
using OIO.Domain.Context.CatalogContext.Aggregates.Items;
using OIO.Domain.Context.CatalogContext.Enums;
using OIO.Domain.Context.NotificationContext.Enums;
using OIO.Domain.Context.UserContext.Aggregates.Users;
using OIO.Domain.Context.UserContext.Enums;
using OIO.Domain.Context.WarehouseContext.Aggregates.InboundShipments;
using OIO.Domain.Context.WarehouseContext.Aggregates.InboundShipments.Events;

namespace OIO.Application.Context.WarehouseContext.EventHandlers;

internal sealed class InboundShipmentArrivedEventHandler(
    IDbContext dbContext,
    ISender sender,
    ILogger<InboundShipmentArrivedEventHandler> logger)
    : INotificationHandler<InboundShipmentArrivedEvent>
{
    public async Task Handle(InboundShipmentArrivedEvent notification, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(notification.InboundShipmentId, out var shipmentIdValue))
            return;

        var shipmentId = OIO.Domain.Context.WarehouseContext.ValueObjects.Ids.InboundShipmentId.From(shipmentIdValue);
        var shipment = await dbContext.Set<InboundShipment>()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == shipmentId, cancellationToken);

        if (shipment is null)
            return;

        var item = await dbContext.Set<Item>()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == shipment.ItemId, cancellationToken);

        if (item is null || item.Status != ItemStatus.PendingVerify)
            return;

        var recipientIds = await dbContext.Set<User>()
            .AsNoTracking()
            .Where(x =>
                x.Status == UserStatus.Active &&
                x.Roles.Any(r =>
                    r.RoleName == App.Roles.Catalogs.Inspector ||
                    r.RoleName == App.Roles.Catalogs.Admin))
            .Select(x => x.Id.Value)
            .ToListAsync(cancellationToken);

        foreach (var recipientId in recipientIds.Distinct())
        {
            await NotificationDispatch.DispatchAsync(
                sender,
                logger,
                new CreateNotificationCommand(
                    UserId: recipientId,
                    NotificationType: "warehouse",
                    EventType: "inbound_shipment_arrived_for_inspection",
                    Title: "Co hang cho kiem dinh",
                    Message: $"San pham \"{item.Title.Value}\" da den kho va dang cho inspection.",
                    Priority: NotificationPriority.Normal,
                    EntityType: "InboundShipment",
                    EntityId: Guid.Parse(notification.InboundShipmentId),
                    Metadata: NotificationDispatch.SerializeMetadata(new
                    {
                        inboundShipmentId = notification.InboundShipmentId,
                        itemId = shipment.ItemId,
                        carrierTrackingNumber = notification.CarrierTrackingNumber
                    })),
                cancellationToken);
        }

        // Notify Seller
        await NotificationDispatch.DispatchAsync(
            sender,
            logger,
            new CreateNotificationCommand(
                UserId: shipment.SellerId.Value,
                NotificationType: "warehouse",
                EventType: "inbound_arrived",
                Title: "Goi hang da den kho",
                Message: $"Goi hang {shipment.ClientOrderCode} cua ban da den kho OIO va dang cho kiem dinh.",
                Priority: NotificationPriority.Normal,
                EntityType: "InboundShipment",
                EntityId: shipment.Id.Value),
            cancellationToken);
    }
}
