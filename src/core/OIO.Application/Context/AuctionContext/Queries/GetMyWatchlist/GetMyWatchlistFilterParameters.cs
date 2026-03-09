using OIO.Application.Abstractions.Commons;

namespace OIO.Application.Context.AuctionContext.Queries.GetMyWatchlist;

public record GetMyWatchlistFilterParameters : PagedParameters, ISortByParameter
{
    public string? AuctionStatus { get; init; }
    public string? SortBy { get; init; }
}