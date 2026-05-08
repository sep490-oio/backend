using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Data;
using OIO.Domain.Context.OrderContext.Aggregates.Orders;
using OIO.Domain.Context.OrderContext.Aggregates.Orders.Events;
using OIO.Domain.Context.OrderContext.ValueObjects.Ids;
using OIO.Domain.Context.UserContext.Aggregates.Users;

namespace OIO.Application.Context.UserContext.EventHandlers;

/// <summary>
/// Reacts to <see cref="OrderCompletedEvent"/> by incrementing the seller's
/// <see cref="SellerProfile.TotalSalesCount"/> and <see cref="SellerProfile.TotalSalesAmount"/>.
/// 
/// Sale is recorded on Order.Completed (not Auction.Sold) because Completed means
/// the buyer has accepted the item and escrow is released — the transaction is fully settled.
/// </summary>
internal sealed class RecordSellerSaleOnOrderCompletedHandler
    : INotificationHandler<OrderCompletedEvent>
{
    private readonly IDbContext _dbContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly ILogger<RecordSellerSaleOnOrderCompletedHandler> _logger;

    public RecordSellerSaleOnOrderCompletedHandler(
        IDbContext dbContext,
        IUnitOfWork unitOfWork,
        IClock clock,
        ILogger<RecordSellerSaleOnOrderCompletedHandler> logger)
    {
        _dbContext = dbContext;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _logger = logger;
    }

    public async Task Handle(OrderCompletedEvent notification, CancellationToken cancellationToken)
    {
        var sellerId = OIO.Domain.Context.UserContext.ValueObjects.Ids.UserId.From(
            Guid.Parse(notification.SellerId));

        // Load the order to get the sale amount
        var orderId = OrderId.From(Guid.Parse(notification.OrderId));
        var order = await _dbContext.Set<Order>()
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken);

        if (order is null)
        {
            _logger.LogWarning(
                "RecordSellerSale: order {OrderId} not found; skipping sale recording.",
                notification.OrderId);
            return;
        }

        var saleAmount = order.Pricing.ItemPrice.Amount;

        // Load seller profile (tracked — we mutate it)
        var sellerProfile = await _dbContext.Set<SellerProfile>()
            .FirstOrDefaultAsync(p => p.Id == sellerId, cancellationToken);

        if (sellerProfile is null)
        {
            _logger.LogWarning(
                "RecordSellerSale: seller profile {SellerId} not found; skipping sale recording.",
                notification.SellerId);
            return;
        }

        sellerProfile.RecordSale(saleAmount, _clock.UtcNow);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "metric=seller_sales_recorded_total SellerId={SellerId} OrderId={OrderId} Amount={Amount}: " +
            "seller profile sales count incremented to {Count}.",
            sellerId.Value, orderId.Value, saleAmount, sellerProfile.TotalSalesCount);
    }
}
