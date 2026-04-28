using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OIO.Application.Abstractions.Data;
using OIO.Application.Context.NotificationContext;
using OIO.Application.Context.NotificationContext.Commands.CreateNotification;
using OIO.Application.Context.OrderContext.Services;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions.Events;
using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;
using OIO.Domain.Context.NotificationContext.Enums;
using OIO.Domain.Context.OrderContext.Aggregates.Orders;
using OIO.Domain.Context.OrderContext.Enums;
using OIO.Domain.Context.UserContext.Aggregates.Users;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;

namespace OIO.Application.Context.AuctionContext.EventHandlers;

internal sealed class AuctionSoldEventHandler
    : INotificationHandler<AuctionSoldEvent>
{
    private const int PaymentDeadlineHours = 48;

    private readonly IDbContext _dbContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ISender _sender;
    private readonly IWinnerOrderProvisioner _winnerOrderProvisioner;
    private readonly ILogger<AuctionSoldEventHandler> _logger;

    public AuctionSoldEventHandler(
        IDbContext dbContext,
        IUnitOfWork unitOfWork,
        ISender sender,
        IWinnerOrderProvisioner winnerOrderProvisioner,
        ILogger<AuctionSoldEventHandler> logger)
    {
        _dbContext = dbContext;
        _unitOfWork = unitOfWork;
        _sender = sender;
        _winnerOrderProvisioner = winnerOrderProvisioner;
        _logger = logger;
    }

    public async Task Handle(AuctionSoldEvent notification, CancellationToken cancellationToken)
    {
        var auctionId = AuctionId.From(Guid.Parse(notification.AuctionId));
        var winnerId = UserId.From(Guid.Parse(notification.WinnerId));
        var sellerId = UserId.From(Guid.Parse(notification.SellerId));

        var auction = await _dbContext.GetByIdAsync<Auction, AuctionId>(
            id: auctionId,
            queryBuilder: query => query
                .AsNoTracking()
                .Include(a => a.Watchers)
                .Include(a => a.Bids)
                .Include(a => a.Item)
                .AsSplitQuery(),
            cancellationToken: cancellationToken);

        // Canonical item lifecycle: mark the sold item as sold in the same
        // transaction as the auction transitions to sold. This replaces the
        // previous job-only path; the auto-complete job remains as an
        // idempotent fallback.
        if (auction?.Item is not null)
        {
            var trackedItem = await _dbContext.Set<OIO.Domain.Context.CatalogContext.Aggregates.Items.Item>()
                .FirstOrDefaultAsync(i => i.Id == auction.Item.Id, cancellationToken);
            if (trackedItem is not null && trackedItem.Status != OIO.Domain.Context.CatalogContext.Enums.ItemStatus.Sold)
            {
                var markResult = trackedItem.MarkSold(DateTime.UtcNow);
                if (markResult.IsFailure)
                {
                    // Bug #5 fix: previously the result was discarded, leading to split-brain
                    // where Auction=Sold but Item stayed in prior state with no signal.
                    // Throw so the outbox processor retries this handler. If the item is
                    // legitimately in a non-transitionable terminal state (Removed), a future
                    // run will keep failing until ops intervene — visible via dashboard alerts.
                    _logger.LogError(
                        "AuctionSoldEventHandler: failed to mark Item={ItemId} as Sold after Auction={AuctionId} sold. Item.Status={Status}, Error={Error}",
                        trackedItem.Id.Value, auctionId.Value, trackedItem.Status.Id, markResult.Error.Message);
                    throw new InvalidOperationException(
                        $"AuctionSold-Item sync failed for Item {trackedItem.Id.Value}: {markResult.Error.Message}");
                }
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
        }

        var winner = await _dbContext.GetByIdAsync<User, UserId>(
            id: winnerId,
            queryBuilder: query => query
                .AsNoTracking()
                .Include(u => u.Profile)
                .Include(u => u.Addresses),
            cancellationToken: cancellationToken);

        if (auction is null || winner is null)
        {
            _logger.LogWarning(
                "AuctionSoldEventHandler: missing data for auction {AuctionId}.",
                notification.AuctionId);
            return;
        }

        var winnerDisplayName = AuctionNotificationDisplayNames.Resolve(winner);
        var orderResult = await _winnerOrderProvisioner.EnsureAsync(
            auctionId: auctionId.Value,
            winnerId: winnerId.Value,
            sellerId: sellerId.Value,
            finalPrice: notification.FinalPrice,
            currency: notification.Currency,
            occurredAt: notification.OccurredAt,
            ct: cancellationToken);
        var order = orderResult.IsSuccess ? orderResult.Value : null;

        var winnerMetadata = NotificationDispatch.SerializeMetadata(new
        {
            auctionId = auctionId.Value,
            itemId = auction.ItemId.Value,
            finalPrice = notification.FinalPrice,
            currency = notification.Currency,
            totalBids = notification.TotalBids,
            orderId = order?.Id.Value,
            orderNumber = order?.OrderNumber.Value,
            orderStatus = order?.Status.Id,
            paymentDueAt = order?.Status == OrderStatus.PendingPayment
                ? order.PaymentDueAt
                : null,
            paidAt = order?.PaidAt
        });

        var winnerActions = order?.Status == OrderStatus.PendingPayment
            ? NotificationDispatch.SerializeMetadata(new[]
            {
                new
                {
                    type = "checkout_order",
                    label = "Thanh toan ngay",
                    method = "POST",
                    endpoint = "api/payments/checkout",
                    payload = new
                    {
                        orderId = order.Id.Value
                    }
                }
            })
            : null;

        await NotificationDispatch.DispatchAsync(
            _sender,
            _logger,
            new CreateNotificationCommand(
                UserId: winnerId.Value,
                NotificationType: "auction",
                EventType: "auction_won",
                Title: "Ban da thang dau gia",
                Message: BuildWinnerMessage(auction, notification, order),
                Priority: NotificationPriority.High,
                EntityType: "Auction",
                EntityId: auctionId.Value,
                Metadata: winnerMetadata,
                Actions: winnerActions),
            cancellationToken);

        await NotificationDispatch.DispatchAsync(
            _sender,
            _logger,
            new CreateNotificationCommand(
                UserId: sellerId.Value,
                NotificationType: "auction",
                EventType: "auction_sold",
                Title: "Phien dau gia da co nguoi thang",
                Message:
                    $"Phien dau gia \"{auction.Item.Title.Value}\" da ban thanh cong. " +
                    $"Nguoi thang: {winnerDisplayName}. Gia cuoi: {NotificationDispatch.FormatAmount(notification.FinalPrice, notification.Currency)}.",
                Priority: NotificationPriority.High,
                EntityType: "Auction",
                EntityId: auctionId.Value,
                Metadata: NotificationDispatch.SerializeMetadata(new
                {
                    auctionId = auctionId.Value,
                    itemId = auction.ItemId.Value,
                    winnerId = winnerId.Value,
                    winnerDisplayName,
                    finalPrice = notification.FinalPrice,
                    currency = notification.Currency,
                    totalBids = notification.TotalBids,
                    orderId = order?.Id.Value,
                    orderNumber = order?.OrderNumber.Value
                })),
            cancellationToken);

        var watcherUserIds = auction.Watchers
            .Where(w => w.NotifyOnEnd && w.UserId != winnerId && w.UserId != sellerId)
            .Select(w => w.UserId.Value)
            .Distinct()
            .ToList();

        foreach (var watcherUserId in watcherUserIds)
        {
            await NotificationDispatch.DispatchAsync(
                _sender,
                _logger,
                new CreateNotificationCommand(
                    UserId: watcherUserId,
                    NotificationType: "auction",
                    EventType: "auction_ended",
                    Title: "Phien dau gia da ket thuc",
                    Message:
                        $"Phien dau gia \"{auction.Item.Title.Value}\" da ket thuc. " +
                        $"Nguoi thang: {winnerDisplayName}. Gia cuoi: {NotificationDispatch.FormatAmount(notification.FinalPrice, notification.Currency)}.",
                    Priority: NotificationPriority.Normal,
                    EntityType: "Auction",
                    EntityId: auctionId.Value,
                    Metadata: NotificationDispatch.SerializeMetadata(new
                    {
                        auctionId = auctionId.Value,
                        itemId = auction.ItemId.Value,
                        winnerId = winnerId.Value,
                        winnerDisplayName,
                        finalPrice = notification.FinalPrice,
                        currency = notification.Currency,
                        orderId = order?.Id.Value
                    })),
                cancellationToken);
        }

        // Notify losing bidders (participants who bid but didn't win)
        var losingBidderIds = auction.Bids
            .Select(b => b.BidderId)
            .Distinct()
            .Where(id => id != winnerId && id != sellerId)
            .Select(id => id.Value)
            .ToList();

        foreach (var losingBidderId in losingBidderIds)
        {
            // Skip if already notified as a watcher
            if (watcherUserIds.Contains(losingBidderId))
                continue;

            await NotificationDispatch.DispatchAsync(
                _sender,
                _logger,
                new CreateNotificationCommand(
                    UserId: losingBidderId,
                    NotificationType: "auction",
                    EventType: "auction_lost",
                    Title: "Phien dau gia da ket thuc",
                    Message:
                        $"Phien dau gia \"{auction.Item.Title.Value}\" da ket thuc. " +
                        $"Ban khong thang. Tien dat coc se duoc hoan tra.",
                    Priority: NotificationPriority.Normal,
                    EntityType: "Auction",
                    EntityId: auctionId.Value,
                    Metadata: NotificationDispatch.SerializeMetadata(new
                    {
                        auctionId = auctionId.Value,
                        finalPrice = notification.FinalPrice,
                        currency = notification.Currency,
                        depositRefund = true
                    })),
                cancellationToken);
        }

        _logger.LogInformation(
            "AuctionSold notifications created. Auction={AuctionId}, Winner={WinnerId}, Order={OrderId}, Watchers={WatcherCount}, LosingBidders={LosingCount}.",
            notification.AuctionId,
            notification.WinnerId,
            order?.Id.Value,
            watcherUserIds.Count,
            losingBidderIds.Count);
    }

    private static string BuildWinnerMessage(
        Auction auction,
        AuctionSoldEvent notification,
        Order? order)
    {
        var prefix =
            $"Ban da thang phien dau gia \"{auction.Item.Title.Value}\" voi muc gia " +
            $"{NotificationDispatch.FormatAmount(notification.FinalPrice, notification.Currency)}. ";

        if (order is null)
            return prefix + "Don hang dang cho khoi tao thanh toan.";

        if (order.Status == OrderStatus.Paid)
            return prefix + $"Don hang {order.OrderNumber.Value} da duoc thanh toan thanh cong.";

        return prefix +
               $"Don hang {order.OrderNumber.Value} da duoc tao. Vui long thanh toan trong {PaymentDeadlineHours} gio.";
    }
}
