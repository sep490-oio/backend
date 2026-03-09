using OIO.Application.Abstractions.Commons;

namespace OIO.Application.Context.AuctionContext.Queries.GetAuctionBids;

public record GetAuctionBidsFilterParameters : PagedParameters, ISortByParameter
{
    public string? SortBy { get; init; }
}