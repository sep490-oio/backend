using OIO.Application.Abstractions.Commons;

namespace OIO.Application.Context.WarehouseContext.Queries.GetWarehouseItems;

public record GetWarehouseItemsQueryFilter : PagedParameters
{
    public string? Status { get; init; }
    public Guid? StorageLocationId { get; init; }
    public Guid? ItemId { get; init; }
    public Guid? InboundShipmentId { get; init; }
    public string? SearchTerm { get; init; }
    public Guid? SellerId { get; init; }
}
