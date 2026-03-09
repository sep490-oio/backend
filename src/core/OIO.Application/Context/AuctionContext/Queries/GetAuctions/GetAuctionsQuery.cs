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
    public ViolationsError Validate()
    {
        return GetAuctionsQuery.Check()
            .WithOwnerName("GetAuctions")
            .Field(Parameters.Status)
            .WhenHasValue(x => x.InSet(AuctionStatus.All.Select(status => status.Id)))
            .Field(Parameters.CategoryId)
            .WhenHasValue(x => x.NotEmptyGuid())
            .Field(Parameters.SortBy)
            .WhenHasValue(x => x.Must(AuctionMappings.AuctionListItemDtoSortMapping.ValidateMappings));
    }
}