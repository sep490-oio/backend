using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.ModerationContext.DTOs;
using OIO.Application.Context.ModerationContext.Mappings;
using OIO.Application.Context.OrderContext.DTOs;
using OIO.Application.Context.OrderContext.Mappings;
using OIO.Application.Context.OrderContext.Queries.Admin.GetCompletedAuctions;
using OIO.Application.Context.WarehouseContext.DTOs;
using OIO.Application.Context.WarehouseContext.Mappings;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions;
using OIO.Domain.Context.ModerationContext.Aggregates;
using OIO.Domain.Context.OrderContext.Aggregates.Orders;
using OIO.Domain.Context.OrderContext.ValueObjects.Ids;
using OIO.Domain.Context.PaymentContext.Aggregates.Transactions;
using OIO.Domain.Context.PaymentContext.Enums;
using OIO.Domain.Context.UserContext.Aggregates.Users;
using OIO.Domain.Context.WarehouseContext.Aggregates.WarehouseItems;
using OIO.Domain.SeedWork.Errors;
using SellerDirectShipmentEntity = OIO.Domain.Context.OrderContext.Aggregates.SellerDirectShipments.SellerDirectShipment;

namespace OIO.Application.Context.OrderContext.Queries.Admin.GetAdminOrderById;

public sealed record GetAdminOrderByIdQuery(Guid OrderId) : IQuery<AdminOrderDetailDto>;

internal sealed class GetAdminOrderByIdQueryHandler(
    IDbContext dbContext)
    : IQueryHandler<GetAdminOrderByIdQuery, AdminOrderDetailDto>
{
    public async Task<Result<AdminOrderDetailDto, Error>> Handle(
        GetAdminOrderByIdQuery request,
        CancellationToken cancellationToken)
    {
        var order = await dbContext.Set<Order>()
            .AsNoTracking()
            .Include(x => x.Return)
                .ThenInclude(r => r!.Evidence)
            .Include(x => x.Escrows)
            .Include(x => x.OutboundShipments)
            .FirstOrDefaultAsync(x => x.Id == OrderId.From(request.OrderId), cancellationToken);

        if (order is null)
            return Error.NotFound("Order.NotFound", "Order was not found.");

        // No BuyerId/SellerId check — admin access enforced at endpoint level.

        // Load auction + item for summary
        var auction = await dbContext.Set<Auction>()
            .AsNoTracking()
            .Include(a => a.Item)
                .ThenInclude(i => i.Media)
            .FirstOrDefaultAsync(a => a.Id == order.AuctionId, cancellationToken);

        var itemSummary = order.ToItemSummary(auction);

        // Load warehouse item for fulfillment metadata
        WarehouseItem? warehouseItem = null;
        if (auction?.Item is not null)
        {
            warehouseItem = await dbContext.Set<WarehouseItem>()
                .AsNoTracking()
                .Where(wi => wi.ItemId == auction.Item.Id.Value)
                .OrderByDescending(wi => wi.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);
        }
        var sellerFulfillment = order.BuildSellerFulfillment(warehouseItem);

        // Display names
        var users = await dbContext.Set<User>()
            .AsNoTracking()
            .Include(u => u.SellerProfile)
            .Where(u => u.Id == order.BuyerId || u.Id == order.SellerId)
            .ToListAsync(cancellationToken);
        var buyer = users.FirstOrDefault(u => u.Id == order.BuyerId);
        var seller = users.FirstOrDefault(u => u.Id == order.SellerId);
        var buyerDisplayName = GetCompletedAuctionsQueryHandler.ResolveUserDisplayName(buyer);
        var sellerDisplayName = GetCompletedAuctionsQueryHandler.ResolveSellerDisplayName(seller);

        // Transactions for payment breakdown
        var orderIdValue = order.Id;
        var orderTransactions = await dbContext.Set<Transaction>()
            .AsNoTracking()
            .Where(t => t.OrderId == orderIdValue && t.Status == TransactionStatus.Completed)
            .ToListAsync(cancellationToken);

        // Direct shipment
        var directShipment = await dbContext.Set<SellerDirectShipmentEntity>()
            .AsNoTracking()
            .Include(s => s.Evidence)
            .FirstOrDefaultAsync(s => s.OrderId == order.Id, cancellationToken);

        var orderDto = order.ToDto(
            item: itemSummary,
            sellerFulfillment: sellerFulfillment,
            buyerDisplayName: buyerDisplayName,
            sellerDisplayName: sellerDisplayName,
            orderTransactions: orderTransactions,
            directShipment: directShipment);

        // Outbound shipment
        OutboundShipmentDto? outboundDto = null;
        var latestShipment = order.OutboundShipments
            .OrderByDescending(s => s.CreatedAt)
            .FirstOrDefault();
        if (latestShipment is not null)
            outboundDto = latestShipment.ToDto();

        // Escrow summary
        AdminOrderEscrowSummaryDto? escrowSummary = null;
        if (order.Escrows.Count > 0)
        {
            var totalHeld = order.Escrows.Where(e => e.Status.Id == "holding").Sum(e => e.Amount.Amount);
            var totalReleased = order.Escrows.Where(e => e.Status.Id == "released_to_seller").Sum(e => e.Amount.Amount);
            var totalRefunded = order.Escrows.Where(e => e.Status.Id == "refunded_to_buyer").Sum(e => e.Amount.Amount);
            escrowSummary = new AdminOrderEscrowSummaryDto(totalHeld, totalReleased, totalRefunded, order.Currency);
        }

        // Monitoring alerts
        var orderGuid = order.Id.Value;
        var monitoringAlerts = await dbContext.Set<MonitoringAlert>()
            .AsNoTracking()
            .Where(x => x.EntityType == "Order" && x.EntityId == orderGuid)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => x.ToDto())
            .ToListAsync(cancellationToken);

        return new AdminOrderDetailDto(
            Order: orderDto,
            OutboundShipment: outboundDto,
            MonitoringAlerts: monitoringAlerts,
            EscrowSummary: escrowSummary);
    }
}
