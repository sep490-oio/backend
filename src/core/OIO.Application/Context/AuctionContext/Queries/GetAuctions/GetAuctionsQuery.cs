using OIO.Application.Abstractions.Commons;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.AuctionContext.DTOs;
using OIO.Application.Context.AuctionContext.Mappings;
using OIO.Domain.Context.AuctionContext.Enums;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.AuctionContext.Queries.GetAuctions;

public sealed record GetAuctionsQuery(GetAuctionsFilterParameters Parameters)
    : IQuery<PagedList<AuctionListItemDto>>, IHasValidate
{
    /// <summary>
    /// Values accepted by <see cref="GetAuctionsFilterParameters.StatusGroup"/>.
    /// Kept alongside the handler's switch so validation and dispatch stay in lock-step.
    /// The empty string is accepted (same semantics as <c>null</c>) so FE can emit a
    /// literal empty <c>statusGroup=</c> without tripping a 400.
    /// </summary>
    public static readonly IReadOnlyCollection<string> AllowedStatusGroups =
        new[] { string.Empty, "active", "scheduled", "sold", "failed" };

    public ViolationsError Validate()
    {
        return GetAuctionsQuery.Check()
            .WithOwnerName("GetAuctions")
            .Field(Parameters.Status)
            .WhenHasValue(x => x.InSet(AuctionStatus.All.Select(status => status.Id)))
            .Field(Parameters.StatusGroup)
            .WhenHasValue(x => x.InSet(AllowedStatusGroups))
            .Field(Parameters.CategoryId)
            .WhenHasValue(x => x.NotEmptyGuid())
            .Field(Parameters.SortBy)
            .WhenHasValue(x => x.Must(AuctionMappings.AuctionListItemDtoSortMapping.ValidateMappings));
    }
}