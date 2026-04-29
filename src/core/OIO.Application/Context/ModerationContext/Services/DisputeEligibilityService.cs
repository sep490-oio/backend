using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Data;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions;
using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;
using OIO.Domain.Context.ModerationContext;
using OIO.Domain.Context.OrderContext.Aggregates.Orders;
using OIO.Domain.Context.OrderContext.ValueObjects.Ids;
using OIO.Domain.Context.PaymentContext.Aggregates.Transactions;
using OIO.Domain.Context.PaymentContext.ValueObjects.Ids;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.Context.WarehouseContext.Aggregates.InboundShipments;
using OIO.Domain.Context.WarehouseContext.Aggregates.OutboundShipments;
using OIO.Domain.Context.WarehouseContext.Aggregates.WarehouseItems;
using OIO.Domain.Context.WarehouseContext.ValueObjects.Ids;

namespace OIO.Application.Context.ModerationContext.Services;

/// <summary>
/// Application-side orchestrator: derives the caller's role against a target entity
/// (DB-backed) and applies the auction-only timing gate. Eligibility matrix lives in
/// <see cref="DisputeEligibilityRule"/> (Domain layer).
/// </summary>
internal sealed class DisputeEligibilityService(
    IDbContext dbContext) : IDisputeEligibilityService
{
    public async Task<string?> ResolveRoleAsync(
        Guid userId,
        string targetType,
        Guid entityId,
        CancellationToken ct)
    {
        return targetType switch
        {
            DisputeEligibilityRule.TargetAuction => await ResolveAuctionRoleAsync(userId, entityId, ct),
            DisputeEligibilityRule.TargetOrder => await ResolveOrderRoleAsync(userId, entityId, ct),
            DisputeEligibilityRule.TargetPayment => await ResolvePaymentRoleAsync(userId, entityId, ct),
            DisputeEligibilityRule.TargetShipment => await ResolveShipmentRoleAsync(userId, entityId, ct),
            DisputeEligibilityRule.TargetWarehouseItem => await ResolveWarehouseItemRoleAsync(userId, entityId, ct),
            _ => null,
        };
    }

    public async Task<bool> IsTimingAllowedAsync(
        string targetType,
        Guid entityId,
        CancellationToken ct)
    {
        // Only auction has a timing gate today; all other targets pass.
        if (targetType != DisputeEligibilityRule.TargetAuction) return true;

        var status = await dbContext.Set<Auction>()
            .AsNoTracking()
            .Where(a => a.Id == AuctionId.From(entityId))
            .Select(a => a.Status.Id)
            .FirstOrDefaultAsync(ct);

        if (status is null) return false;
        return DisputeEligibilityRule.AuctionAllowedStatuses.Contains(status);
    }

    private async Task<string?> ResolveAuctionRoleAsync(
        Guid userId,
        Guid auctionId,
        CancellationToken ct)
    {
        var aId = AuctionId.From(auctionId);
        var uId = UserId.From(userId);

        // Project just the participant ids — avoids loading the full aggregate or its bids.
        var participants = await dbContext.Set<Auction>()
            .AsNoTracking()
            .Where(a => a.Id == aId)
            .Select(a => new { SellerId = a.Item.SellerId, a.WinnerId })
            .FirstOrDefaultAsync(ct);

        if (participants is null) return null;

        if (participants.SellerId == uId) return DisputeEligibilityRule.RoleSeller;
        if (participants.WinnerId == uId) return DisputeEligibilityRule.RoleWinner;

        // Separate AnyAsync over Bid set — no Include nav, per Architect nice-to-have #8.
        var hasBid = await dbContext.Set<Bid>()
            .AsNoTracking()
            .AnyAsync(b => b.AuctionId == aId && b.BidderId == uId, ct);

        return hasBid
            ? DisputeEligibilityRule.RoleLosingBidder
            : DisputeEligibilityRule.RoleObserver;
    }

    private async Task<string?> ResolveOrderRoleAsync(
        Guid userId,
        Guid orderId,
        CancellationToken ct)
    {
        var oId = OrderId.From(orderId);
        var uId = UserId.From(userId);

        var participants = await dbContext.Set<Order>()
            .AsNoTracking()
            .Where(o => o.Id == oId)
            .Select(o => new { o.BuyerId, o.SellerId })
            .FirstOrDefaultAsync(ct);

        if (participants is null) return null;

        if (participants.BuyerId == uId) return DisputeEligibilityRule.RoleBuyer;
        if (participants.SellerId == uId) return DisputeEligibilityRule.RoleSeller;
        return null;
    }

    private async Task<string?> ResolvePaymentRoleAsync(
        Guid userId,
        Guid paymentId,
        CancellationToken ct)
    {
        var tId = TransactionId.From(paymentId);
        var uId = UserId.From(userId);

        var ownerId = await dbContext.Set<Transaction>()
            .AsNoTracking()
            .Where(t => t.Id == tId)
            .Select(t => (UserId?)t.UserId)
            .FirstOrDefaultAsync(ct);

        if (ownerId is null) return null;
        return ownerId == uId ? DisputeEligibilityRule.RoleOwner : null;
    }

    private async Task<string?> ResolveShipmentRoleAsync(
        Guid userId,
        Guid shipmentId,
        CancellationToken ct)
    {
        var sId = OutboundShipmentId.From(shipmentId);
        var uId = UserId.From(userId);

        // Resolve via the related order's buyer.
        var orderRef = await dbContext.Set<OutboundShipment>()
            .AsNoTracking()
            .Where(s => s.Id == sId)
            .Select(s => (OrderId?)s.OrderId)
            .FirstOrDefaultAsync(ct);

        if (orderRef is null) return null;

        var buyerId = await dbContext.Set<Order>()
            .AsNoTracking()
            .Where(o => o.Id == orderRef.Value)
            .Select(o => (UserId?)o.BuyerId)
            .FirstOrDefaultAsync(ct);

        if (buyerId is null) return null;
        return buyerId == uId ? DisputeEligibilityRule.RoleBuyer : null;
    }

    private async Task<string?> ResolveWarehouseItemRoleAsync(
        Guid userId,
        Guid warehouseItemId,
        CancellationToken ct)
    {
        var wId = WarehouseItemId.From(warehouseItemId);
        var uId = UserId.From(userId);

        var inboundRef = await dbContext.Set<WarehouseItem>()
            .AsNoTracking()
            .Where(w => w.Id == wId)
            .Select(w => (InboundShipmentId?)w.InboundShipmentId)
            .FirstOrDefaultAsync(ct);

        if (inboundRef is null) return null;

        var sellerId = await dbContext.Set<InboundShipment>()
            .AsNoTracking()
            .Where(s => s.Id == inboundRef.Value)
            .Select(s => (UserId?)s.SellerId)
            .FirstOrDefaultAsync(ct);

        if (sellerId is null) return null;
        return sellerId == uId ? DisputeEligibilityRule.RoleSeller : null;
    }
}
