using OIO.Application.Abstractions.Commons;

namespace OIO.Application.Context.AuctionContext.Queries.GetAuctions;

public record GetAuctionsFilterParameters : PagedParameters, ISortByParameter
{
    public string? Status { get; init; }

    /// <summary>
    /// Coarse-grained grouping of <see cref="Status"/> used by the redesigned public
    /// browse. Accepted values: <c>null</c>, <c>""</c>, <c>"active"</c>,
    /// <c>"scheduled"</c>, <c>"sold"</c>, <c>"failed"</c>. When both <see cref="Status"/>
    /// and <see cref="StatusGroup"/> are provided, <see cref="StatusGroup"/> wins
    /// (FE dual-write intentionally sends both during the back-compat window).
    /// </summary>
    public string? StatusGroup { get; init; }

    public Guid? CategoryId { get; init; }

    public string? Search { get; init; }

    public decimal? MinPrice { get; init; }

    public decimal? MaxPrice { get; init; }

    public string? SortBy { get; init; }

    public int? EndingWithinHours { get; init; }

    public bool? IsFeatured { get; init; }

    public string? AuctionType { get; init; }
}