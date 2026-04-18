using OIO.Application.Abstractions.Commons;

namespace OIO.Application.Context.AuctionContext.Queries.GetAuctions;

public record GetAuctionsFilterParameters : PagedParameters, ISortByParameter
{
    public string? Status { get; init; }

    public Guid? CategoryId { get; init; }

    public string? Search { get; init; }

    public decimal? MinPrice { get; init; }

    public decimal? MaxPrice { get; init; }

    public string? SortBy { get; init; }

    public int? EndingWithinHours { get; init; }

    public bool? IsFeatured { get; init; }

    public string? AuctionType { get; init; }
}