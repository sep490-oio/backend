using MediatR;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Data;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions.Events;
using OIO.Domain.Context.ModerationContext.Aggregates;
using OIO.Domain.Context.ModerationContext.Enums;

namespace OIO.Application.Context.AuctionContext.EventHandlers;

internal sealed class AuctionPaymentDefaultedMonitoringEventHandler(
    IDbContext dbContext,
    IUnitOfWork unitOfWork)
    : INotificationHandler<AuctionPaymentDefaultedEvent>
{
    public async Task Handle(AuctionPaymentDefaultedEvent notification, CancellationToken cancellationToken)
    {
        var auctionId = Guid.Parse(notification.AuctionId);

        var exists = await dbContext.Set<MonitoringAlert>()
            .AsNoTracking()
            .AnyAsync(
                x => x.EntityType == "Auction" &&
                     x.EntityId == auctionId &&
                     x.AlertType == "auction_payment_defaulted" &&
                     x.Status == AlertStatus.Open,
                cancellationToken);

        if (exists)
            return;

        dbContext.Insert(MonitoringAlert.Create(
            entityType: "Auction",
            entityId: auctionId,
            alertType: "auction_payment_defaulted",
            severity: AlertSeverity.High,
            payload: System.Text.Json.JsonSerializer.Serialize(new
            {
                auctionId,
                sellerId = notification.SellerId,
                defaultedWinnerId = notification.DefaultedWinnerId
            }),
            nowUtc: notification.OccurredAt));

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}

internal sealed class AuctionTerminatedMonitoringEventHandler(
    IDbContext dbContext,
    IUnitOfWork unitOfWork)
    : INotificationHandler<AuctionTerminatedEvent>
{
    public async Task Handle(AuctionTerminatedEvent notification, CancellationToken cancellationToken)
    {
        var auctionId = Guid.Parse(notification.AuctionId);

        var exists = await dbContext.Set<MonitoringAlert>()
            .AsNoTracking()
            .AnyAsync(
                x => x.EntityType == "Auction" &&
                     x.EntityId == auctionId &&
                     x.AlertType == "auction_terminated" &&
                     x.Status == AlertStatus.Open,
                cancellationToken);

        if (exists)
            return;

        dbContext.Insert(MonitoringAlert.Create(
            entityType: "Auction",
            entityId: auctionId,
            alertType: "auction_terminated",
            severity: AlertSeverity.High,
            payload: System.Text.Json.JsonSerializer.Serialize(new
            {
                auctionId,
                sellerId = notification.SellerId,
                reason = notification.Reason
            }),
            nowUtc: notification.OccurredAt));

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
