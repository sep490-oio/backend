using OIO.Application.Abstractions.Commons;

namespace OIO.Application.Context.AuctionContext.Queries.GetMyBids;

public record GetMyBidsFilterParameters : PagedParameters, ISortByParameter
{
    public string? Status { get; init; }
    public string? SortBy { get; init; }
}