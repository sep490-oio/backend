using OIO.Application.Abstractions.Commons;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.AuctionContext.DTOs;
using OIO.Application.Context.AuctionContext.Mappings;
using OIO.Domain.Context.AuctionContext.Enums;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.AuctionContext.Queries.GetMyWatchlist;

public sealed record GetMyAuctionWatchlistQuery(GetMyWatchlistFilterParameters Parameters) 
    : IQuery<PagedList<MyAuctionWatchlistDto>>, IHasValidate
{
    public ViolationsError Validate()
    {
        return GetMyAuctionWatchlistQuery.Check()
            .WithOwnerName("GetMyAuctionWatchlist")
            .Field(Parameters.AuctionStatus)
            .WhenHasValue(x => x.InSet(AuctionStatus.All.Select(y => y.Id)))
            .Field(Parameters.SortBy)
            .WhenHasValue(x => x.Must(AuctionWatcherMappings.MyAuctionWatchlistDtoSortMapping.ValidateMappings));
    }
}