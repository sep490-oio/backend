namespace OIO.Application.Context.WarehouseContext.DTOs;

/// <summary>
/// Row for the warehouse-staff outbound booking queue — one entry per processing
/// order whose item lives in the platform warehouse (<c>warehouse_managed</c>) and
/// that has no active outbound shipment yet. Warehouse staff use this list to pick
/// the next order to book an outbound shipment for.
/// </summary>
public sealed record WarehouseStaffOutboundQueueItemDto(
    Guid OrderId,
    string OrderNumber,
    string OrderStatus,
    DateTime? OrderPaidAt,
    Guid AuctionId,
    Guid WarehouseItemId,
    string? ItemTitle,
    string? ItemPrimaryImageUrl,
    string? BuyerRecipientName,
    string? BuyerShippingAddress,
    string? SellerDisplayName,
    string? StorageLocationLabel);

/// <summary>
/// Full detail payload used by the warehouse-staff outbound booking screen.
/// Carries everything the booking form needs so the FE doesn't have to join
/// <c>useWarehouseItems</c> / order details on the client.
/// </summary>
public sealed record WarehouseStaffOutboundOrderDetailDto(
    // Order
    Guid OrderId,
    string OrderNumber,
    string OrderStatus,
    DateTime? OrderPaidAt,
    // Warehouse item
    Guid WarehouseItemId,
    string WarehouseItemStatus,
    string? StorageLocationLabel,
    // Item
    string? ItemTitle,
    string? ItemPrimaryImageUrl,
    decimal ItemPriceDefault,
    // Bidder shipping
    string? RecipientName,
    string? RecipientPhone,
    string? Street,
    string? Ward,
    string? District,
    string? Province,
    string? PostalCode,
    string? ComposedAddress,
    // Seller
    string? SellerDisplayName,
    // Package defaults (no ShippingProviderConfig defaults exist for box size;
    // use 1000g / 20x15x10cm as sensible fallbacks — carriers accept these.)
    int WeightGrams,
    int LengthCm,
    int WidthCm,
    int HeightCm,
    decimal InsuranceValueDefault,
    // COD is 0 because the order is already paid; staff can override if needed.
    decimal CodAmountDefault,
    // Default active shipping provider — surfaced so the FE can label the
    // platform-managed option (e.g. "GHN (platform default)"). Null if no
    // default provider is configured.
    string? DefaultProviderCode = null,
    string? DefaultProviderLabel = null);
