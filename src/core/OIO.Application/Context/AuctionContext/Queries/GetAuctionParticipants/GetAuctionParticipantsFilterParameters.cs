using OIO.Application.Abstractions.Commons;
using OIO.Domain.Context.AuctionContext.Enums;

namespace OIO.Application.Context.AuctionContext.Queries.GetAuctionParticipants;

public record GetAuctionParticipantsFilterParameters : PagedParameters, ISortByParameter
{
    public string? JoinStatus { get; init; }
    public string? QualificationStatus { get; init; }
    public string? SortBy { get; init; }
}
