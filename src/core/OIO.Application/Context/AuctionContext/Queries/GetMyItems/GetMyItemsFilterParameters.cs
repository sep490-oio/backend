using OIO.Application.Abstractions.Commons;

namespace OIO.Application.Context.AuctionContext.Queries.GetMyItems;

public record GetMyItemsFilterParameters : PagedParameters, ISortByParameter
{
    public string? SortBy { get; init; }
}