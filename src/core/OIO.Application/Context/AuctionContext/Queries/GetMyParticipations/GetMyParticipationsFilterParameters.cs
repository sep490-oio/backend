using OIO.Application.Abstractions.Commons;

namespace OIO.Application.Context.AuctionContext.Queries.GetMyParticipations;

public record GetMyParticipationsFilterParameters : PagedParameters, ISortByParameter
{
    /// <summary>
    /// Filter: all | active | won | lost | deposit_only
    /// </summary>
    public string? Status { get; init; }
    public string? SortBy { get; init; }
}
