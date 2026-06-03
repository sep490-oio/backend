using OIO.Application.Abstractions.Commons;

namespace OIO.Application.Context.WarehouseContext.Queries.GetMyInboundShipments;

public record GetMyInboundShipmentsFilterParameters : PagedParameters
{
    public Guid? ItemId { get; init; }
}
