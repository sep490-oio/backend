using CSharpFunctionalExtensions;
using OIO.Application.Abstractions.Commons;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.AuctionContext.DTOs;
using OIO.Application.Context.AuctionContext.Mappings;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.AuctionContext.Queries.GetAuctionParticipants;

public sealed record GetAuctionParticipantsQuery(
    Guid AuctionId,
    GetAuctionParticipantsFilterParameters Parameters)
    : IQuery<PagedList<AuctionParticipantListItemDto>>, IHasValidate
{
    public ViolationsError Validate()
    {
        return GetAuctionParticipantsQuery.Check()
            .WithOwnerName("GetAuctionParticipants")
            .Field(AuctionId)
            .NotEmptyGuid()
            .Field(Parameters.SortBy)
            .WhenHasValue(x => x.Must(AuctionParticipantMappings.AuctionParticipantListItemDtoSortMapping.ValidateMappings));
    }
}
