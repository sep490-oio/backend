using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Commons;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.OrderContext.Services;
using OIO.Application.Context.PaymentContext.DTOs;
using OIO.Application.Context.UserContext.Services;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions;
using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;
using OIO.Domain.Context.CatalogContext.Aggregates.Items;
using OIO.Domain.Context.ModerationContext.Aggregates.Disputes;
using OIO.Domain.Context.OrderContext.Aggregates.Orders;
using OIO.Domain.Context.OrderContext.Enums;
using OIO.Domain.Context.OrderContext.ValueObjects.Ids;
using OIO.Domain.Context.PaymentContext.Aggregates.Escrows;
using OIO.Domain.Context.PaymentContext.Enums;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.PaymentContext.Queries.GetSellerEscrowLedger;

public sealed record GetSellerEscrowLedgerQuery(int Skip = 0, int Take = 50)
    : IQuery<IReadOnlyList<SellerEscrowLedgerRowDto>>;

internal sealed class GetSellerEscrowLedgerQueryHandler
    : IQueryHandler<GetSellerEscrowLedgerQuery, IReadOnlyList<SellerEscrowLedgerRowDto>>
{
    private readonly IDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly IRuntimeSettings _runtimeSettings;

    public GetSellerEscrowLedgerQueryHandler(
        IDbContext dbContext,
        ICurrentUser currentUser,
        IRuntimeSettings runtimeSettings)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _runtimeSettings = runtimeSettings;
    }

    public async Task<Result<IReadOnlyList<SellerEscrowLedgerRowDto>, Error>> Handle(
        GetSellerEscrowLedgerQuery request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;
        var settlementOptions = _runtimeSettings.Settlement;
        var orderOptions = _runtimeSettings.Order;

        var skip = Math.Max(0, request.Skip);
        var take = Math.Clamp(request.Take, 1, 200);

        // Pull escrow + order rows. Auction/Item/Dispute are looked up via
        // separate dictionaries to avoid forcing navigation properties that
        // don't exist on Order (Order has AuctionId only).
        var escrowRows = await _dbContext.Set<Escrow>()
            .AsNoTracking()
            .Include(e => e.Order)
            .Where(e => e.Order.SellerId == userId)
            .OrderByDescending(e => e.HeldAt)
            .Skip(skip)
            .Take(take)
            .Select(e => new EscrowLedgerProjection
            {
                EscrowId = e.Id.Value,
                OrderId = e.Order.Id.Value,
                OrderNumber = e.Order.OrderNumber.Value,
                AuctionId = e.Order.AuctionId.Value,
                GrossAmount = e.Amount.Amount,
                Currency = e.Currency,
                OrderStatusId = e.Order.Status.Id,
                EscrowStatusId = e.Status.Id,
                IsPlatformVerifiedItem = e.Order.IsPlatformVerifiedItem,
                DisputedAt = e.Order.DisputedAt,
                DeliveredAt = e.Order.DeliveredAt,
                DecisionWindowEndsAt = e.Order.DecisionWindowEndsAt,
                PaidAt = e.Order.PaidAt,
                ShipByAt = e.Order.ShipByAt,
                ReleasedAt = e.ReleasedAt,
                ReleasedToId = e.ReleasedTo.Id,
            })
            .ToListAsync(cancellationToken);

        if (escrowRows.Count == 0)
            return Result.Success<IReadOnlyList<SellerEscrowLedgerRowDto>, Error>(Array.Empty<SellerEscrowLedgerRowDto>());

        var auctionIdValues = escrowRows.Select(r => AuctionId.From(r.AuctionId)).Distinct().ToArray();
        var orderIdValues = escrowRows.Select(r => OrderId.From(r.OrderId)).Distinct().ToArray();

        // Auction → Item title (Order has AuctionId; Item lives via Auction.ItemId).
        var auctionItemTitles = await _dbContext.Set<Auction>()
            .AsNoTracking()
            .Where(a => auctionIdValues.Contains(a.Id))
            .Join(
                _dbContext.Set<Item>().AsNoTracking(),
                auction => auction.ItemId,
                item => item.Id,
                (auction, item) => new { AuctionId = auction.Id.Value, ItemTitle = item.Title.Value })
            .ToDictionaryAsync(x => x.AuctionId, x => x.ItemTitle, cancellationToken);

        // Latest dispute per order (sellers may see prior closed disputes).
        var disputes = await _dbContext.Set<Dispute>()
            .AsNoTracking()
            .Where(d => orderIdValues.Contains(d.OrderId))
            .OrderByDescending(d => d.CreatedAt)
            .Select(d => new
            {
                OrderId = d.OrderId.Value,
                DisputeId = d.Id.Value,
                Status = d.Status.Id,
                d.CreatedAt
            })
            .ToListAsync(cancellationToken);

        var disputeByOrder = disputes
            .GroupBy(d => d.OrderId)
            .ToDictionary(g => g.Key, g => g.OrderBy(x => x.CreatedAt).Last());

        var rows = new List<SellerEscrowLedgerRowDto>(escrowRows.Count);
        foreach (var row in escrowRows)
        {
            var includeInspection = row.IsPlatformVerifiedItem;
            var settlementResult = EscrowSettlementService.CalculateSellerSettlement(
                settlementOptions,
                row.GrossAmount,
                row.Currency,
                includeInspectionFee: includeInspection);
            if (settlementResult.IsFailure)
                return settlementResult.Error;

            var breakdown = settlementResult.Value;
            var isHolding = row.EscrowStatusId == EscrowStatus.Holding.Id;

            var holdReason = ResolveHoldReason(
                isHolding: isHolding,
                disputedAt: row.DisputedAt,
                orderStatusId: row.OrderStatusId,
                isVerified: row.IsPlatformVerifiedItem);

            var expectedReleaseAt = ResolveExpectedReleaseAt(
                row,
                orderOptions.ReturnDecisionWindowDays);

            disputeByOrder.TryGetValue(row.OrderId, out var dispute);

            decimal? actualReleased = null;
            if (!isHolding && row.ReleasedToId == "seller")
                actualReleased = breakdown.SellerNetAmount;

            rows.Add(new SellerEscrowLedgerRowDto(
                OrderId: row.OrderId,
                OrderNumber: row.OrderNumber,
                AuctionId: row.AuctionId,
                ItemTitle: auctionItemTitles.TryGetValue(row.AuctionId, out var title) ? title : string.Empty,
                GrossPaidAmount: row.GrossAmount,
                Currency: row.Currency,
                OrderStatus: row.OrderStatusId,
                EscrowStatus: row.EscrowStatusId,
                HoldReason: holdReason,
                BuyerPaidAt: row.PaidAt,
                ExpectedReleaseAt: expectedReleaseAt,
                DecisionWindowEndsAt: row.DecisionWindowEndsAt,
                IsPlatformVerifiedItem: row.IsPlatformVerifiedItem,
                PlatformCommissionAmount: breakdown.PlatformCommission,
                InspectionFeeAmount: breakdown.InspectionFee,
                EstimatedNetPayout: breakdown.SellerNetAmount,
                ActualReleasedAmount: actualReleased,
                DisputeId: dispute?.DisputeId,
                DisputeStatus: dispute?.Status));
        }

        return Result.Success<IReadOnlyList<SellerEscrowLedgerRowDto>, Error>(rows);
    }

    private static string? ResolveHoldReason(
        bool isHolding,
        DateTime? disputedAt,
        string orderStatusId,
        bool isVerified)
    {
        if (!isHolding)
            return null;

        if (disputedAt is not null)
            return "Frozen by dispute";

        if (orderStatusId == OrderStatus.Delivered.Id)
            return "Awaiting buyer acceptance";

        if (orderStatusId == OrderStatus.Paid.Id ||
            orderStatusId == OrderStatus.Processing.Id ||
            orderStatusId == OrderStatus.PickedUp.Id ||
            orderStatusId == OrderStatus.OnDelivering.Id ||
            orderStatusId == OrderStatus.Shipped.Id)
        {
            return "Awaiting delivery";
        }

        if (isVerified && orderStatusId == OrderStatus.PendingPayment.Id)
            return "Inspection in progress";

        return null;
    }

    private static DateTime? ResolveExpectedReleaseAt(
        EscrowLedgerProjection row,
        int returnDecisionWindowDays)
    {
        if (row.EscrowStatusId != EscrowStatus.Holding.Id)
            return null;

        // Strongest signal: an explicit decision window deadline already
        // stamped on the order takes precedence.
        if (row.DecisionWindowEndsAt is not null)
            return row.DecisionWindowEndsAt;

        // Delivered but no decision window stamped yet — fall back to
        // delivery + configured ReturnDecisionWindowDays.
        if (row.DeliveredAt is not null)
            return row.DeliveredAt.Value.AddDays(returnDecisionWindowDays);

        // Pre-delivery: surface the seller ship-by SLA when present so the
        // UI can hint at the next inflection point.
        return row.ShipByAt;
    }

    private sealed class EscrowLedgerProjection
    {
        public Guid EscrowId { get; init; }
        public Guid OrderId { get; init; }
        public string OrderNumber { get; init; } = string.Empty;
        public Guid AuctionId { get; init; }
        public decimal GrossAmount { get; init; }
        public string Currency { get; init; } = string.Empty;
        public string OrderStatusId { get; init; } = string.Empty;
        public string EscrowStatusId { get; init; } = string.Empty;
        public bool IsPlatformVerifiedItem { get; init; }
        public DateTime? DisputedAt { get; init; }
        public DateTime? DeliveredAt { get; init; }
        public DateTime? DecisionWindowEndsAt { get; init; }
        public DateTime? PaidAt { get; init; }
        public DateTime? ShipByAt { get; init; }
        public DateTime? ReleasedAt { get; init; }
        public string ReleasedToId { get; init; } = string.Empty;
    }
}
