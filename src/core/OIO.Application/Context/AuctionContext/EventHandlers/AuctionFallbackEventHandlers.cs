using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OIO.Application.Abstractions.Data;
using OIO.Application.Context.AuctionContext.Hubs;
using OIO.Application.Context.AuctionContext.Services;
using OIO.Application.Context.NotificationContext;
using OIO.Application.Context.NotificationContext.Commands.CreateNotification;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions.Events;
using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;
using OIO.Domain.Context.NotificationContext.Enums;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;

namespace OIO.Application.Context.AuctionContext.EventHandlers;

internal sealed class AuctionPaymentDefaultedEventHandler(
    IDbContext dbContext,
    ISender sender,
    ILogger<AuctionPaymentDefaultedEventHandler> logger)
    : INotificationHandler<AuctionPaymentDefaultedEvent>
{
    public async Task Handle(AuctionPaymentDefaultedEvent notification, CancellationToken cancellationToken)
    {
        var auction = await dbContext.Set<Auction>()
            .AsNoTracking()
            .Include(x => x.Item)
            .FirstOrDefaultAsync(
                x => x.Id == AuctionId.From(Guid.Parse(notification.AuctionId)),
                cancellationToken);

        if (auction is null)
            return;

        await NotificationDispatch.DispatchAsync(
            sender,
            logger,
            new CreateNotificationCommand(
                UserId: Guid.Parse(notification.SellerId),
                NotificationType: "auction",
                EventType: "auction_payment_defaulted",
                Title: "Winner khong thanh toan dung han",
                Message: $"Phien dau gia \"{auction.Item.Title.Value}\" da bi qua han thanh toan. Ban co the moi runner-up hoac relist.",
                Priority: NotificationPriority.High,
                EntityType: "Auction",
                EntityId: auction.Id.Value,
                Actions: NotificationDispatch.SerializeMetadata(new[]
                {
                    new
                    {
                        type = "offer_next_rank",
                        label = "Moi runner-up",
                        method = "POST",
                        endpoint = $"api/auctions/{auction.Id.Value}/runner-up-offers"
                    },
                    new
                    {
                        type = "relist",
                        label = "Relist",
                        method = "POST",
                        endpoint = $"api/auctions/{auction.Id.Value}/relist"
                    }
                })),
            cancellationToken);
    }
}

internal sealed class AuctionRunnerUpOfferedEventHandler(
    IDbContext dbContext,
    ISender sender,
    ILogger<AuctionRunnerUpOfferedEventHandler> logger)
    : INotificationHandler<AuctionRunnerUpOfferedEvent>
{
    public async Task Handle(AuctionRunnerUpOfferedEvent notification, CancellationToken cancellationToken)
    {
        var auction = await dbContext.Set<Auction>()
            .AsNoTracking()
            .Include(x => x.Item)
            .FirstOrDefaultAsync(
                x => x.Id == AuctionId.From(Guid.Parse(notification.AuctionId)),
                cancellationToken);

        if (auction is null)
            return;

        await NotificationDispatch.DispatchAsync(
            sender,
            logger,
            new CreateNotificationCommand(
                UserId: Guid.Parse(notification.BidderId),
                NotificationType: "auction",
                EventType: "runner_up_offer_received",
                Title: "Ban duoc moi nhan quyen mua",
                Message: $"Ban duoc moi nhan quyen mua tu phien dau gia \"{auction.Item.Title.Value}\". Vui long phan hoi truoc {notification.ExpiresAt:dd/MM/yyyy HH:mm}.",
                Priority: NotificationPriority.High,
                EntityType: "Auction",
                EntityId: auction.Id.Value,
                Actions: NotificationDispatch.SerializeMetadata(new[]
                {
                    new
                    {
                        type = "accept_runner_up_offer",
                        label = "Chap nhan",
                        method = "POST",
                        endpoint = $"api/auctions/{auction.Id.Value}/runner-up-offers/respond",
                        payload = new { accept = true }
                    },
                    new
                    {
                        type = "decline_runner_up_offer",
                        label = "Tu choi",
                        method = "POST",
                        endpoint = $"api/auctions/{auction.Id.Value}/runner-up-offers/respond",
                        payload = new { accept = false }
                    }
                })),
            cancellationToken);
    }
}

internal sealed class AuctionRunnerUpOfferRespondedEventHandler(
    ISender sender,
    ILogger<AuctionRunnerUpOfferRespondedEventHandler> logger)
    : INotificationHandler<AuctionRunnerUpOfferRespondedEvent>
{
    public async Task Handle(AuctionRunnerUpOfferRespondedEvent notification, CancellationToken cancellationToken)
    {
        await NotificationDispatch.DispatchAsync(
            sender,
            logger,
            new CreateNotificationCommand(
                UserId: Guid.Parse(notification.BidderId),
                NotificationType: "auction",
                EventType: "runner_up_offer_responded",
                Title: "Phan hoi runner-up da duoc ghi nhan",
                Message: $"He thong da ghi nhan phan hoi {notification.Response} cho loi moi runner-up.",
                Priority: NotificationPriority.Normal,
                EntityType: "Auction",
                EntityId: Guid.Parse(notification.AuctionId)),
            cancellationToken);
    }
}

internal sealed class AuctionTerminatedEventHandler(
    IDbContext dbContext,
    ISender sender,
    IAuctionNotificationService hubNotifier,
    ILogger<AuctionTerminatedEventHandler> logger)
    : INotificationHandler<AuctionTerminatedEvent>
{
    public async Task Handle(AuctionTerminatedEvent notification, CancellationToken cancellationToken)
    {
        var auctionId = Guid.Parse(notification.AuctionId);
        var auction = await dbContext.Set<Auction>()
            .AsNoTracking()
            .Include(x => x.Item)
            .Include(x => x.Watchers)
            .FirstOrDefaultAsync(x => x.Id == AuctionId.From(auctionId), cancellationToken);

        if (auction is null)
            return;

        await hubNotifier.NotifyAuctionCancelledAsync(
            auctionId,
            new AuctionCancelledNotification(auctionId, notification.Reason),
            cancellationToken);

        var recipients = auction.Watchers.Select(x => x.UserId)
            .Append(auction.Item.SellerId)
            .Distinct()
            .ToList();

        if (auction.WinnerId.HasValue)
            recipients.Add(auction.WinnerId.Value);

        foreach (var recipient in recipients.Distinct())
        {
            await NotificationDispatch.DispatchAsync(
                sender,
                logger,
                new CreateNotificationCommand(
                    UserId: recipient.Value,
                    NotificationType: "auction",
                    EventType: "auction_terminated",
                    Title: "Phien dau gia da bi terminate",
                    Message: $"Phien dau gia \"{auction.Item.Title.Value}\" da bi terminate do tinh huong khan cap. Ly do: {notification.Reason}",
                    Priority: NotificationPriority.High,
                    EntityType: "Auction",
                    EntityId: auctionId),
                cancellationToken);
        }
    }
}
