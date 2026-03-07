using OIO.Application.Abstractions.Commons;

namespace OIO.Application.Context.AuctionContext.Queries.Filters;

public record AuctionFilterParameters : PagedParameters, ISortByParameter
{
    public string? Status { get; init; }

    public Guid? CategoryId { get; init; }

    public string? Search { get; init; }

    public decimal? MinPrice { get; init; }

    public decimal? MaxPrice { get; init; }

    public string? SortBy { get; init; }

    public int? EndingWithinHours { get; init; }

    public bool? IsFeatured { get; init; }
}

public record MyAuctionFilterParameters : PagedParameters, ISortByParameter
{
    public string? Status { get; init; }
    public string? SortBy { get; init; }
}

public record MyBidFilterParameters : PagedParameters, ISortByParameter
{
    public string? Status { get; init; }
    public string? SortBy { get; init; }
}

public record MyWatchlistFilterParameters : PagedParameters, ISortByParameter
{
    /// <summary>
    /// Filter: active, ended, all
    /// </summary>
    public string? AuctionStatus { get; init; }
    public string? SortBy { get; init; }
}