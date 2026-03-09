using OIO.Application.Abstractions.Commons;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.AuctionContext.DTOs;
using OIO.Application.Context.AuctionContext.Mappings;
using OIO.Domain.Context.AuctionContext.Enums;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.AuctionContext.Queries.GetMyBids;

public sealed record GetMyBidsQuery(
    GetMyBidsFilterParameters Parameters) : IQuery<PagedList<MyBidDto>>, IHasValidate
{
    public ViolationsError Validate()
    {
        return GetMyBidsQuery.Check()
            .WithOwnerName("GetMyBids")
            .Field(Parameters.Status)
            .WhenHasValue(x => x.InSet(BidStatus.All.Select(s => s.Id)))
            .Field(Parameters.SortBy)
            .WhenHasValue(x => x.Must(BidMappings.MyBidDtoSortMapping.ValidateMappings));
    }
}