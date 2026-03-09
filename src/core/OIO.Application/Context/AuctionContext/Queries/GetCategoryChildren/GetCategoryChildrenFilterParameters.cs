using OIO.Application.Abstractions.Commons;

namespace OIO.Application.Context.AuctionContext.Queries.GetCategoryChildren;

public record GetCategoryChildrenFilterParameters : PagedParameters, ISortByParameter
{
    public string? SortBy { get; init; }
}