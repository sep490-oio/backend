using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Search;
using OIO.Infrastructure.Settings;
using OIO.Domain.Context.CatalogContext.Aggregates.Items;
using OIO.Domain.Context.CatalogContext.Enums;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions;
using OIO.Domain.Context.OrderContext.Aggregates.Orders;
using OIO.Domain.Context.UserContext.Aggregates.Users;
using OIO.Domain.Context.WarehouseContext.Aggregates.InboundShipments;
using OIO.Domain.Context.WarehouseContext.Aggregates.OutboundShipments;
using OIO.Domain.Context.CatalogContext.ValueObjects.Ids;
using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.Context.OrderContext.ValueObjects.Ids;
using OIO.Domain.Context.AuctionContext.Enums;
using OIO.Domain.Context.WarehouseContext.Aggregates.WarehouseItems;
using OIO.Domain.Context.WarehouseContext.ValueObjects.Ids;
using OIO.Domain.Context.WarehouseContext.Aggregates.WarehouseItems;

namespace OIO.Infrastructure.Elasticsearch;

public class ElasticsearchSyncService : IElasticsearchSyncService
{
    private readonly IDbContext _dbContext;
    private readonly IElasticsearchService _esService;
    private readonly IClock _clock;
    private readonly ElasticsearchSettings _settings;

    public ElasticsearchSyncService(
        IDbContext dbContext,
        IElasticsearchService esService,
        IClock clock,
        IOptions<ElasticsearchSettings> settings)
    {
        _dbContext = dbContext;
        _esService = esService;
        _clock = clock;
        _settings = settings.Value;
    }

    public async Task SyncItemAsync(Guid itemId, CancellationToken cancellationToken = default)
    {
        var item = await _dbContext.Set<Item>()
            .Include(i => i.Category)
            .Include(i => i.Media)
            .FirstOrDefaultAsync(i => i.Id == ItemId.From(itemId), cancellationToken);

        if (item == null) return;

        // Only index Active or InAuction items
        if (item.Status != ItemStatus.Active && item.Status != ItemStatus.InAuction)
        {
            await _esService.DeleteDocumentAsync(item.Id.ToString(), _settings.ItemsIndex, cancellationToken);
            return;
        }

        var sellerProfile = await _dbContext.Set<SellerProfile>()
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == item.SellerId, cancellationToken);

        // Fetch live auction (Scheduled or Active) for this item
        var liveAuction = await _dbContext.Set<Auction>()
            .AsNoTracking()
            .Where(a => a.ItemId == item.Id &&
                (a.Status == AuctionStatus.Scheduled || a.Status == AuctionStatus.Active ||
                 a.Status == AuctionStatus.Approved))
            .OrderByDescending(a => a.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        var doc = new ItemSearchDocument
        {
            Id = item.Id.ToString(),
            DisplayName = item.Title.Value,
            Description = item.Description,
            ThumbnailUrl = item.Media.FirstOrDefault(m => m.IsPrimary)?.Info.SecureUrl,
            CreatedAt = item.CreatedAt,
            ModifiedAt = item.ModifiedAt,
            Title = item.Title.Value,
            CategoryName = item.Category?.Name ?? "Uncategorized",
            CategoryId = item.CategoryId?.ToString() ?? string.Empty,
            Condition = item.Condition.Id,
            Status = item.Status.Id,
            SellerId = item.SellerId.ToString(),
            Quantity = item.Quantity,
            RequiresPlatformInspection = item.RequiresPlatformInspection,
            SellerName = sellerProfile?.StoreName ?? "Private Seller",
            HasLiveAuction = liveAuction != null,
            // Auction summary
            AuctionId = liveAuction?.Id.ToString(),
            AuctionStatus = liveAuction?.Status.Id,
            AuctionType = liveAuction?.AuctionType?.Id,
            AuctionCurrentPrice = liveAuction?.Pricing.CurrentAmount,
            AuctionCurrency = liveAuction?.Pricing.Currency.Id,
            AuctionStartTime = liveAuction?.Info?.StartTime,
            AuctionEndTime = liveAuction?.Info?.EndTime,
            Images = item.Media.Select(m => new ImageSearchDocument
            {
                Id = m.Id.ToString(),
                Url = m.Info.SecureUrl ?? string.Empty,
                PublicId = m.StorageRef.PublicId,
                ResourceType = m.ResourceType,
                IsPrimary = m.IsPrimary,
                SortOrder = m.SortOrder,
                FileName = m.Info.FileName,
                Bytes = m.Info.Bytes,
                Format = m.Info.Format,
                Width = m.Info.Width,
                Height = m.Info.Height
            }).ToList(),
            Suggest = new[] { item.Title.Value, item.Category?.Name }.Where(s => !string.IsNullOrEmpty(s)).ToList()!
        };

        await _esService.IndexDocumentAsync(doc, _settings.ItemsIndex, cancellationToken);
    }

    public async Task SyncAuctionAsync(Guid auctionId, CancellationToken cancellationToken = default)
    {
        var auction = await _dbContext.Set<Auction>()
            .Include(a => a.Item)
                .ThenInclude(i => i.Media)
            .Include(a => a.Item)
                .ThenInclude(i => i.Category)
            .Include(a => a.BuyNowReservations)
            .FirstOrDefaultAsync(a => a.Id == AuctionId.From(auctionId), cancellationToken);

        if (auction == null) return;

        var sellerProfile = await _dbContext.Set<SellerProfile>()
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == auction.Item.SellerId, cancellationToken);

        // Only index Scheduled or Active auctions
        if (auction.Status != AuctionStatus.Scheduled && auction.Status != AuctionStatus.Active)
        {
            await _esService.DeleteDocumentAsync(auction.Id.ToString(), _settings.AuctionsIndex, cancellationToken);
            return;
        }

        var nowUtc = _clock.UtcNow;
        var activeReservation = auction.GetActiveBuyNowReservation(nowUtc);

        var doc = new AuctionSearchDocument
        {
            Id = auction.Id.ToString(),
            DisplayName = auction.Item.Title.Value,
            Description = auction.Item.Description,
            ThumbnailUrl = auction.Item.Media.OrderBy(m => m.SortOrder).FirstOrDefault(m => m.IsPrimary)?.Info.SecureUrl,
            CreatedAt = auction.CreatedAt,
            ModifiedAt = auction.ModifiedAt,
            Title = auction.Item.Title.Value,
            ItemId = auction.ItemId.ToString(),
            AuctionType = auction.AuctionType?.Id ?? string.Empty,
            CurrentPrice = auction.Pricing.CurrentAmount,
            StartingPrice = auction.Pricing.StartingAmount,
            BuyNowPrice = auction.Pricing.BuyNowAmount,
            IsBuyNowReserved = activeReservation != null,
            Currency = auction.Pricing.Currency.Id,
            Status = auction.Status.Id,
            StartTime = auction.Info?.StartTime,
            EndTime = auction.Info?.EndTime,
            BidCount = auction.BidCount,
            WatchCount = auction.WatchCount,
            ViewCount = auction.ViewCount,
            IsFeatured = auction.IsFeatured,
            Condition = auction.Item.Condition.Id,
            ItemStatus = auction.Item.Status.Id,
            SellerId = auction.Item.SellerId.ToString(),
            SellerName = sellerProfile?.StoreName ?? "Private Seller",
            CategoryName = auction.Item.Category?.Name ?? "Uncategorized",
            Suggest = new List<string> { auction.Item.Title.Value }
        };

        await _esService.IndexDocumentAsync(doc, _settings.AuctionsIndex, cancellationToken);
    }

    public async Task SyncUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Set<User>()
            .Include(u => u.Roles)
            .Include(u => u.Profile)
            .FirstOrDefaultAsync(u => u.Id == UserId.From(userId), cancellationToken);

        if (user == null) return;

        var doc = new UserSearchDocument
        {
            Id = user.Id.ToString(),
            DisplayName = user.UserName.Value,
            CreatedAt = user.CreatedAt,
            ModifiedAt = user.ModifiedAt,
            UserName = user.UserName.Value,
            Email = user.Email.Value,
            PhoneNumber = user.PhoneNumber?.Value,
            FullName = user.Profile.Name.FullName,
            Status = user.Status.Id,
            Roles = user.Roles.Select(r => r.RoleName).ToList()
        };

        await _esService.IndexDocumentAsync(doc, _settings.UsersIndex, cancellationToken);
    }

    public async Task SyncOrderAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        var order = await _dbContext.Set<Order>()
            .FirstOrDefaultAsync(o => o.Id == OrderId.From(orderId), cancellationToken);

        if (order == null) return;

        var doc = new OrderSearchDocument
        {
            Id = order.Id.ToString(),
            DisplayName = $"Order {order.OrderNumber.Value}",
            CreatedAt = order.CreatedAt,
            ModifiedAt = order.ModifiedAt,
            OrderNumber = order.OrderNumber.Value,
            Status = order.Status.Id,
            TotalAmount = order.Pricing.TotalAmount.Amount,
            Currency = order.Currency,
            BuyerId = order.BuyerId.ToString(),
            SellerId = order.SellerId.ToString(),
            AuctionId = order.AuctionId.ToString(),
            Notes = order.Notes
        };

        await _esService.IndexDocumentAsync(doc, _settings.OrdersIndex, cancellationToken);
    }

    public async Task SyncShipmentAsync(Guid shipmentId, bool isOutbound, CancellationToken cancellationToken = default)
    {
        if (isOutbound)
        {
            var shipment = await _dbContext.Set<OutboundShipment>()
                .FirstOrDefaultAsync(s => s.Id == OutboundShipmentId.From(shipmentId), cancellationToken);

            if (shipment == null) return;

            var doc = new ShipmentSearchDocument
            {
                Id = shipment.Id.ToString(),
                DisplayName = $"Outbound {shipment.CarrierTrackingNumber ?? shipment.ClientOrderCode}",
                CreatedAt = shipment.CreatedAt,
                ModifiedAt = shipment.ModifiedAt,
                ShipmentType = "Outbound",
                TrackingNumber = shipment.CarrierTrackingNumber ?? string.Empty,
                ClientOrderCode = shipment.ClientOrderCode,
                Status = shipment.Status.Id,
                ProviderCode = shipment.ProviderCode.Id,
                Suggest = new List<string> { shipment.CarrierTrackingNumber ?? string.Empty, shipment.ClientOrderCode }
            };

            await _esService.IndexDocumentAsync(doc, _settings.ShipmentsIndex, cancellationToken);
        }
        else
        {
            var shipment = await _dbContext.Set<InboundShipment>()
                .FirstOrDefaultAsync(s => s.Id == InboundShipmentId.From(shipmentId), cancellationToken);

            if (shipment == null) return;

            var doc = new ShipmentSearchDocument
            {
                Id = shipment.Id.ToString(),
                DisplayName = $"Inbound {shipment.CarrierTrackingNumber ?? shipment.ClientOrderCode}",
                CreatedAt = shipment.CreatedAt,
                ModifiedAt = shipment.ModifiedAt,
                ShipmentType = "Inbound",
                TrackingNumber = shipment.CarrierTrackingNumber ?? string.Empty,
                ClientOrderCode = shipment.ClientOrderCode,
                Status = shipment.Status.Id,
                ProviderCode = shipment.ProviderCode.Id,
                SenderName = shipment.SenderName
            };

            await _esService.IndexDocumentAsync(doc, _settings.ShipmentsIndex, cancellationToken);
        }
    }

    public async Task SyncWarehouseItemAsync(Guid warehouseItemId, CancellationToken cancellationToken = default)
    {
        var warehouseItem = await _dbContext.Set<WarehouseItem>()
            .FirstOrDefaultAsync(w => w.Id == WarehouseItemId.From(warehouseItemId), cancellationToken);

        if (warehouseItem == null) return;

        // Fetch the Catalog Item for Title and SellerId
        var catalogItem = await _dbContext.Set<Item>()
            .FirstOrDefaultAsync(i => i.Id == ItemId.From(warehouseItem.ItemId), cancellationToken);

        var doc = new WarehouseItemSearchDocument
        {
            Id = warehouseItem.Id.ToString(),
            DisplayName = catalogItem?.Title.Value ?? $"Warehouse Item {warehouseItem.Id}",
            CreatedAt = warehouseItem.CreatedAt,
            ModifiedAt = warehouseItem.ModifiedAt,
            ItemId = warehouseItem.ItemId.ToString(),
            Title = catalogItem?.Title.Value ?? string.Empty,
            SellerId = catalogItem?.SellerId.ToString() ?? string.Empty,
            Status = warehouseItem.Status.Id,
            StorageLocation = warehouseItem.StorageLocationId?.ToString()
        };

        await _esService.IndexDocumentAsync(doc, _settings.WarehouseIndex, cancellationToken);
    }

    public async Task SyncAllAsync(CancellationToken cancellationToken = default)
    {
        // Items - Only Active or InAuction
        var itemIds = await _dbContext.Set<Item>()
            .Where(i => i.Status == ItemStatus.Active || i.Status == ItemStatus.InAuction)
            .Select(i => i.Id.Value)
            .ToListAsync(cancellationToken);
        foreach (var id in itemIds)
            await SyncItemAsync(id, cancellationToken);

        // Auctions - Only Scheduled and Active
        var auctionIds = await _dbContext.Set<Auction>()
            .Where(a => a.Status == AuctionStatus.Scheduled || a.Status == AuctionStatus.Active)
            .Select(a => a.Id.Value)
            .ToListAsync(cancellationToken);
        foreach (var id in auctionIds)
            await SyncAuctionAsync(id, cancellationToken);

        // Users
        var userIds = await _dbContext.Set<User>()
            .Select(u => u.Id.Value)
            .ToListAsync(cancellationToken);
        foreach (var id in userIds)
            await SyncUserAsync(id, cancellationToken);

        // Orders
        var orderIds = await _dbContext.Set<Order>()
            .Select(o => o.Id.Value)
            .ToListAsync(cancellationToken);
        foreach (var id in orderIds)
            await SyncOrderAsync(id, cancellationToken);

        // Warehouse Items
        var warehouseItemIds = await _dbContext.Set<WarehouseItem>()
            .Select(w => w.Id.Value)
            .ToListAsync(cancellationToken);
        foreach (var id in warehouseItemIds)
            await SyncWarehouseItemAsync(id, cancellationToken);
    }
}
