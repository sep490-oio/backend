using OIO.Application.Abstractions.Commons;

namespace OIO.Application.Context.AuctionContext.Queries.GetMyAuctions;

public record GetMyAuctionsFilterParameters : PagedParameters, ISortByParameter
{
    public string? Status { get; init; }
    public string? SortBy { get; init; }
}