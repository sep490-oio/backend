using OIO.Application.Abstractions.Sorting;
using OIO.Application.Context.AuctionContext.DTOs;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions;

namespace OIO.Application.Context.AuctionContext.Mappings;

public static class AuctionParticipantMappings
{
    public static readonly SortMappingDefinition AuctionParticipantListItemDtoSortMapping = SortMappingBuilder<AuctionParticipantListItemDto, AuctionParticipant>.Create()
        .Map(x => x.RegisteredAt, x => x.JoinedAt)
        .Map(x => x.QualifiedAt, x => x.QualifiedAt)
        .Build();

    public static AuctionParticipantListItemDto ToListItemDto(
        this AuctionParticipant participant,
        string? displayName)
    {
        return new AuctionParticipantListItemDto(
            participant.AuctionId.Value,
            participant.UserId.Value,
            displayName,
            participant.JoinStatus.Id,
            participant.QualificationStatus.Id,
            participant.JoinedAt,
            participant.QualifiedAt,
            null,
            null
        );
    }
}
