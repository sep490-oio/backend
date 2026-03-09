using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Commons;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.AuctionContext.DTOs;
using OIO.Application.Context.AuctionContext.Mappings;
using OIO.Application.Extensions;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions;
using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.AuctionContext.Queries.GetAuctionBids;

internal sealed class GetAuctionBidsQueryHandler
    : IQueryHandler<GetAuctionBidsQuery, PagedList<BidDto>>
{
    private readonly IDbContext _dbContext;
    
    public GetAuctionBidsQueryHandler(
        IDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<PagedList<BidDto>, Error>> Handle(
        GetAuctionBidsQuery request,
        CancellationToken cancellationToken)
    {
        var parameters = request.Parameters;
        
        var auctionId = AuctionId.From(request.AuctionId);

        var query = _dbContext.Set<Bid>()
            .Where(b => b.AuctionId == auctionId)
            .ApplySort(parameters, BidMappings.BidDtoSortMapping);
        
        var totalCount = await query
            .CountAsync(cancellationToken);
        
        var allBids = await query
            .Select(bid => new BidDto(
                Id: bid.Id.Value,
                AuctionId: bid.AuctionId.Value,
                BidderId: bid.BidderId.Value,
                Amount: bid.Amount.ToDto(),
                IsAutoBid: bid.IsAutoBid,
                Status: bid.Status.Id,
                CreatedAt: bid.CreatedAt))
            .ToPagedListAsync(totalCount, parameters, cancellationToken);

        return  allBids;
    }
}