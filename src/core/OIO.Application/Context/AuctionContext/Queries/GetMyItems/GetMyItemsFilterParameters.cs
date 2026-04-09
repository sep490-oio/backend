using OIO.Application.Abstractions.Commons;

namespace OIO.Application.Context.AuctionContext.Queries.GetMyItems;

public record GetMyItemsFilterParameters : PagedParameters, ISortByParameter
{
    public string? SortBy { get; init; }

    /// <summary>
    /// Optional ItemStatus filter (e.g. "pending_verify", "draft"). Validated
    /// against <see cref="OIO.Domain.Context.CatalogContext.Enums.ItemStatus.All"/>.
    /// </summary>
    public string? Status { get; init; }

    /// <summary>
    /// Filters by the canonical persisted flag <c>Item.RequiresPlatformInspection</c>
    /// (set at Submit/Resubmit time). Null = no filter.
    /// </summary>
    public bool? RequiresPlatformInspection { get; init; }

    /// <summary>
    /// When false, returns only items that have NO active inbound shipment —
    /// where "active" = an InboundShipment whose Status is neither Cancelled
    /// nor Failed. When true, returns only items that DO have an active inbound.
    /// Null = no filter. Used by the inbound-book picker so a re-attempt is
    /// allowed after a cancelled/failed inbound.
    /// </summary>
    public bool? HasActiveInbound { get; init; }
}