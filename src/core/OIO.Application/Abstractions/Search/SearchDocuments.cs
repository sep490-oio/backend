using OIO.Application.Abstractions.Search;

namespace OIO.Application.Abstractions.Search;

public class ImageSearchDocument
{
    public string Id { get; init; } = string.Empty;
    public string Url { get; init; } = string.Empty;
    public string PublicId { get; init; } = string.Empty;
    public string ResourceType { get; init; } = string.Empty;
    public bool IsPrimary { get; init; }
    public int SortOrder { get; init; }
    public string? FileName { get; init; }
    public long? Bytes { get; init; }
    public string? Format { get; init; }
    public int? Width { get; init; }
    public int? Height { get; init; }
}

public class ItemSearchDocument : BaseSearchDocument
{
    public ItemSearchDocument() => EntityType = "Item";
    
    public string Title { get; init; } = string.Empty;
    public string CategoryId { get; init; } = string.Empty;
    public string Condition { get; init; } = string.Empty;
    public int Quantity { get; init; }
    public bool RequiresPlatformInspection { get; init; }
    public string SellerName { get; init; } = string.Empty;
    public bool HasLiveAuction { get; init; }
    public List<ImageSearchDocument> Images { get; init; } = [];

    // Live auction summary (mirrors PublicSellerItemAuctionSummaryDto)
    public string? AuctionId { get; init; }
    public string? AuctionStatus { get; init; }
    public string? AuctionType { get; init; }
    public decimal? AuctionCurrentPrice { get; init; }
    public string? AuctionCurrency { get; init; }
    public DateTime? AuctionStartTime { get; init; }
    public DateTime? AuctionEndTime { get; init; }
}

public class AuctionSearchDocument : BaseSearchDocument
{
    public AuctionSearchDocument() => EntityType = "Auction";
    
    public string Title { get; init; } = string.Empty;
    public string ItemId { get; init; } = string.Empty;
    public string AuctionType { get; init; } = string.Empty;
    public decimal CurrentPrice { get; init; }
    public decimal StartingPrice { get; init; }
    public decimal? BuyNowPrice { get; init; }
    public bool IsBuyNowReserved { get; init; }
    public string Currency { get; init; } = "VND";
    public DateTime? StartTime { get; init; }
    public DateTime? EndTime { get; init; }
    public int BidCount { get; init; }
    public int WatchCount { get; init; }
    public int ViewCount { get; init; }
    public bool IsFeatured { get; init; }
    public string Condition { get; init; } = string.Empty;
    public string ItemStatus { get; init; } = string.Empty;
    public List<string> WatcherIds { get; init; } = [];
}

public class UserSearchDocument : BaseSearchDocument
{
    public UserSearchDocument() => EntityType = "User";
    
    public string UserName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string? PhoneNumber { get; init; }
    public string FullName { get; init; } = string.Empty;
    public List<string> Roles { get; init; } = [];
}

public class OrderSearchDocument : BaseSearchDocument
{
    public OrderSearchDocument() => EntityType = "Order";
    
    public string OrderNumber { get; init; } = string.Empty;
    public decimal TotalAmount { get; init; }
    public string Currency { get; init; } = "VND";
    public string BuyerName { get; init; } = string.Empty;
    public string BuyerId { get; init; } = string.Empty;
    public string SellerId { get; init; } = string.Empty;
    public string AuctionId { get; init; } = string.Empty;
    public string? Notes { get; init; }
}

public class ShipmentSearchDocument : BaseSearchDocument
{
    public ShipmentSearchDocument() => EntityType = "Shipment";
    
    public string ShipmentType { get; init; } = string.Empty; // Inbound / Outbound
    public string TrackingNumber { get; init; } = string.Empty;
    public string ClientOrderCode { get; init; } = string.Empty;
    public string ProviderCode { get; init; } = string.Empty;
    public string SenderName { get; init; } = string.Empty;
    public string RecipientName { get; init; } = string.Empty;
}

public class WarehouseItemSearchDocument : BaseSearchDocument
{
    public WarehouseItemSearchDocument() => EntityType = "WarehouseItem";
    
    public string ItemId { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string? StorageLocation { get; init; }
}
