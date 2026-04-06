using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OIO.Application.Abstractions.Data;
using OIO.Application.Context.NotificationContext;
using OIO.Application.Context.NotificationContext.Commands.CreateNotification;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions.Events;
using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;
using OIO.Domain.Context.NotificationContext.Enums;
using OIO.Domain.Context.OrderContext.Aggregates.Orders;
using OIO.Domain.Context.OrderContext.Enums;
using OIO.Domain.Context.OrderContext.ValueObjects;
using OIO.Domain.Context.Shared.ValueObjects;
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
    private readonly ILogger<AuctionSoldEventHandler> _logger;

    public AuctionSoldEventHandler(
        IDbContext dbContext,
        IUnitOfWork unitOfWork,
        ISender sender,
        ILogger<AuctionSoldEventHandler> logger)
    {
        _dbContext = dbContext;
        _unitOfWork = unitOfWork;
        _sender = sender;
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
                .Include(a => a.Item),
            cancellationToken: cancellationToken);

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
        var order = await EnsureOrderAsync(
            auction,
            winner,
            sellerId,
            notification,
            cancellationToken);

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

    private async Task<Order?> EnsureOrderAsync(
        Auction auction,
        User winner,
        UserId sellerId,
        AuctionSoldEvent notification,
        CancellationToken cancellationToken)
    {
        var existingOrder = await _dbContext.Set<Order>()
            .FirstOrDefaultAsync(
                order => order.AuctionId == auction.Id && order.BuyerId == winner.Id,
                cancellationToken);

        if (existingOrder is not null)
            return existingOrder;

        var moneyResult = Money.Create(notification.FinalPrice, notification.Currency);
        if (moneyResult.IsFailure)
        {
            _logger.LogWarning(
                "Unable to create order pricing for auction {AuctionId}. Error={Error}",
                notification.AuctionId,
                moneyResult.Error.Message);
            return null;
        }

        var shippingAddress = winner.Addresses.FirstOrDefault(address => address.IsDefault)
                              ?? winner.Addresses.FirstOrDefault();

        var shippingSnapshot = shippingAddress is null
            ? ShippingSnapshot.Create(
                recipientName: winnerDisplayNameOrUserName(winner),
                phone: null,
                address: "Address pending update",
                ward: null,
                district: null,
                city: null)
            : ShippingSnapshot.Create(
                recipientName: shippingAddress.Recipient.RecipientName,
                phone: shippingAddress.Recipient.Phone.Value,
                address: shippingAddress.Address.Street,
                ward: shippingAddress.Address.Ward,
                district: shippingAddress.Address.District,
                city: shippingAddress.Address.City);

        var pricing = OrderPricing.Create(
            itemPrice: moneyResult.Value,
            shippingFee: 0m,
            platformFee: 0m,
            taxAmount: 0m,
            totalAmount: moneyResult.Value);

        var orderResult = Order.Create(
            auctionId: auction.Id,
            buyerId: winner.Id,
            sellerId: sellerId,
            shipping: shippingSnapshot,
            shippingAddressId: shippingAddress?.Id,
            billingAddressId: shippingAddress?.Id,
            pricing: pricing,
            currency: notification.Currency,
            paymentDueAt: notification.OccurredAt.AddHours(PaymentDeadlineHours),
            nowUtc: notification.OccurredAt,
            notes: shippingAddress is null
                ? "Winner had no default address when order was generated."
                : null);

        if (orderResult.IsFailure)
        {
            _logger.LogWarning(
                "Unable to create order for auction {AuctionId}. Error={Error}",
                notification.AuctionId,
                orderResult.Error.Message);
            return null;
        }

        _dbContext.Insert(orderResult.Value);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return orderResult.Value;
    }

    private static string winnerDisplayNameOrUserName(User winner)
    {
        var displayName = AuctionNotificationDisplayNames.Resolve(winner);
        return string.IsNullOrWhiteSpace(displayName) ? winner.UserName.Value : displayName;
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
