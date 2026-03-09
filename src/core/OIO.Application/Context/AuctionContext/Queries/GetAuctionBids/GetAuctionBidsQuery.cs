using OIO.Application.Abstractions.Commons;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.AuctionContext.DTOs;
using OIO.Application.Context.AuctionContext.Mappings;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.AuctionContext.Queries.GetAuctionBids;

public sealed record GetAuctionBidsQuery(
    Guid AuctionId,
    GetAuctionBidsFilterParameters Parameters) : IQuery<PagedList<BidDto>>, IHasValidate
{
    public ViolationsError Validate()
    {
        return GetAuctionBidsQuery.Check()
            .WithOwnerName("GetAuctionBids")
            .Field(AuctionId)
            .NotEmptyGuid()
            .Field(Parameters.SortBy)
            .WhenHasValue(x => x.Must(BidMappings.BidDtoSortMapping.ValidateMappings));
    }
}