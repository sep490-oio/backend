using OIO.Application.Abstractions.Commons;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.AuctionContext.DTOs;
using OIO.Application.Context.AuctionContext.Mappings;
using OIO.Domain.Context.AuctionContext.Enums;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.AuctionContext.Queries.GetMyAuctions;

public sealed record GetMyAuctionsQuery(GetMyAuctionsFilterParameters Parameters) 
    : IQuery<PagedList<AuctionListItemDto>>, IHasValidate
{
    public ViolationsError Validate()
    {
        return GetMyAuctionsQuery.Check()
            .WithOwnerName("GetMyAuctions")
            .Field(Parameters.Status)
            .WhenHasValue(x => x.InSet(AuctionStatus.All.Select(status => status.Id)))
            .Field(Parameters.SortBy)
            .WhenHasValue(x => x.Must(AuctionMappings.AuctionListItemDtoSortMapping.ValidateMappings));
    }
}