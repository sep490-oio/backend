using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Commons;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.AuctionContext.DTOs;
using OIO.Application.Context.AuctionContext.Mappings;
using OIO.Application.Extensions;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions;
using OIO.Domain.Context.AuctionContext.Errors;
using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.AuctionContext.Queries.GetAuctionBids;

public sealed record GetAuctionBidsQuery(
    Guid AuctionId,
    PagedParameters PagedParameters) : IQuery<PagedList<BidDto>>, IHasValidate
{
    public ViolationsError Validate()
    {
        return GetAuctionBidsQuery.Check()
            .WithOwnerName("GetAuctionBids")
            .Field(AuctionId)
            .NotEmptyGuid();
    }
}

internal sealed class GetAuctionBidsQueryHandler
    : IQueryHandler<GetAuctionBidsQuery, PagedList<BidDto>>
{
    private readonly IDbContext _dbContext;
    private readonly IClock _clock;
    private readonly IAppConfigs _appConfigs;
    
    public GetAuctionBidsQueryHandler(
        IDbContext dbContext,
        IClock clock,
        IAppConfigs appConfigs)
    {
        _dbContext = dbContext;
        _clock = clock;
        _appConfigs = appConfigs;
    }

    public async Task<Result<PagedList<BidDto>, Error>> Handle(
        GetAuctionBidsQuery request,
        CancellationToken cancellationToken)
    {
        var auctionId = AuctionId.From(request.AuctionId);
        
        var allBids = await _dbContext.Set<Bid>()
            .Where(b => b.AuctionId == auctionId)
            .OrderByDescending(b => b.CreatedAt)
            .Page(request.PagedParameters)
            .Select(bid => new BidDto(
                Id: bid.Id.Value,
                AuctionId: bid.AuctionId.Value,
                BidderId: bid.BidderId.Value,
                Amount: bid.Amount.Amount,
                IsAutoBid: bid.IsAutoBid,
                Status: bid.Status.Id,
                CreatedAt: bid.CreatedAt))
            .ToListAsync(cancellationToken);
        
        var totalCount = await _dbContext.Set<Bid>()
            .Where(b => b.AuctionId == auctionId)
            .CountAsync(cancellationToken);

        return  PagedList<BidDto>.ToPagedList(
            allBids,
            totalCount, 
            request.PagedParameters);
    }
}