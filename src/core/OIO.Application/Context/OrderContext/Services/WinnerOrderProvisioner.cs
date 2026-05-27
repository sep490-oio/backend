using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OIO.Application.Abstractions.Data;
using OIO.Application.Context.AuctionContext.EventHandlers;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions;
using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;
using OIO.Domain.Context.OrderContext.Aggregates.Orders;
using OIO.Domain.Context.OrderContext.Enums;
using OIO.Domain.Context.OrderContext.ValueObjects;
using OIO.Domain.Context.Shared.ValueObjects;
using OIO.Domain.Context.UserContext.Aggregates.Users;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.OrderContext.Services;

public interface IWinnerOrderProvisioner
{
    Task<Result<Order, Error>> EnsureAsync(
        Guid auctionId,
        Guid winnerId,
        Guid sellerId,
        decimal finalPrice,
        string currency,
        DateTime occurredAt,
        CancellationToken ct);
}

internal sealed class WinnerOrderProvisioner : IWinnerOrderProvisioner
{
    private const int PaymentDeadlineHours = 48;

    private readonly IDbContext _dbContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<WinnerOrderProvisioner> _logger;

    public WinnerOrderProvisioner(
        IDbContext dbContext,
        IUnitOfWork unitOfWork,
        ILogger<WinnerOrderProvisioner> logger)
    {
        _dbContext = dbContext;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<Order, Error>> EnsureAsync(
        Guid auctionId,
        Guid winnerId,
        Guid sellerId,
        decimal finalPrice,
        string currency,
        DateTime occurredAt,
        CancellationToken ct)
    {
        var auctionIdVo = AuctionId.From(auctionId);
        var winnerIdVo = UserId.From(winnerId);
        var sellerIdVo = UserId.From(sellerId);

        // BUG FIX: Exclude terminal-status orders (Cancelled, Refunded) from the
        // idempotency check. A user may have a Cancelled order from a prior Buy Now
        // that was abandoned. If the same user later wins the auction via bidding,
        // the provisioner must create a fresh PendingPayment order rather than
        // returning the stale Cancelled order (which cannot be paid).
        var existingOrders = await _dbContext.Set<Order>()
            .Where(
                order => order.AuctionId == auctionIdVo
                      && order.BuyerId == winnerIdVo
                      && order.Status != OrderStatus.Cancelled
                      && order.Status != OrderStatus.Refunded)
            .ToListAsync(ct);

        var matchingOrder = existingOrders.FirstOrDefault(o => o.Pricing.ItemPrice.Amount == finalPrice);
        var staleOrders = existingOrders.Where(o => o.Pricing.ItemPrice.Amount != finalPrice).ToList();

        bool hasChanges = false;
        foreach (var staleOrder in staleOrders)
        {
            var cancelResult = staleOrder.Cancel("Superseded by auction win.", occurredAt);
            if (cancelResult.IsSuccess)
                hasChanges = true;
        }

        if (hasChanges)
        {
            await _unitOfWork.SaveChangesAsync(ct);
        }

        if (matchingOrder is not null)
            return matchingOrder;

        var auction = await _dbContext.Set<Auction>()
            .AsNoTracking()
            .Include(x => x.Item)
            .FirstOrDefaultAsync(x => x.Id == auctionIdVo, ct);

        if (auction is null)
        {
            _logger.LogWarning(
                "WinnerOrderProvisioner: missing auction {AuctionId} while creating winner order.",
                auctionId);
            return Error.NotFound("WinnerOrderProvisioner.AuctionNotFound", "Auction not found.");
        }

        var moneyResult = Money.Create(finalPrice, currency);
        if (moneyResult.IsFailure)
        {
            _logger.LogWarning(
                "WinnerOrderProvisioner: unable to build pricing for auction {AuctionId}. Error={Error}",
                auctionId,
                moneyResult.Error.Message);
            return moneyResult.Error;
        }

        var winner = await _dbContext.GetByIdAsync<User, UserId>(
            id: winnerIdVo,
            queryBuilder: query => query
                .AsNoTracking()
                .Include(u => u.Profile)
                .Include(u => u.Addresses),
            cancellationToken: ct);

        if (winner is null)
        {
            _logger.LogWarning(
                "WinnerOrderProvisioner: missing winner {WinnerId} for auction {AuctionId}.",
                winnerId,
                auctionId);
            return Error.NotFound("WinnerOrderProvisioner.WinnerNotFound", "Winner user not found.");
        }

        var shippingAddress = winner.Addresses.FirstOrDefault(address => address.IsDefault)
                              ?? winner.Addresses.FirstOrDefault();

        var shippingSnapshot = shippingAddress is null
            ? ShippingSnapshot.Create(
                recipientName: ResolveRecipientName(winner),
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
            auctionId: auctionIdVo,
            buyerId: winnerIdVo,
            sellerId: sellerIdVo,
            shipping: shippingSnapshot,
            shippingAddressId: shippingAddress?.Id,
            billingAddressId: shippingAddress?.Id,
            pricing: pricing,
            currency: currency,
            paymentDueAt: occurredAt.AddHours(PaymentDeadlineHours),
            nowUtc: occurredAt,
            isPlatformVerifiedItem: auction.Item.RequiresPlatformInspection,
            notes: shippingAddress is null
                ? "Winner had no default address when order was generated."
                : null);

        if (orderResult.IsFailure)
        {
            _logger.LogWarning(
                "WinnerOrderProvisioner: unable to create order for auction {AuctionId}. Error={Error}",
                auctionId,
                orderResult.Error.Message);
            return orderResult.Error;
        }

        _dbContext.Insert(orderResult.Value);
        await _unitOfWork.SaveChangesAsync(ct);

        return orderResult.Value;
    }

    private static string ResolveRecipientName(User winner)
    {
        var displayName = AuctionNotificationDisplayNames.Resolve(winner);
        return string.IsNullOrWhiteSpace(displayName) ? winner.UserName.Value : displayName;
    }
}
