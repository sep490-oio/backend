using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Commons;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.AuctionContext.DTOs;
using OIO.Application.Context.AuctionContext.EventHandlers;
using OIO.Application.Context.AuctionContext.Mappings;
using OIO.Application.Extensions;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions;
using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;
using OIO.Domain.Context.UserContext.Aggregates.Users;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
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

        var bidItems = await query
            .Page(parameters)
            .ToListAsync(cancellationToken);

        var bidderIds = bidItems
            .Select(b => b.BidderId)
            .Distinct()
            .Select(id => UserId.From(id.Value))
            .ToList();

        var bidderUsers = await _dbContext.Set<User>()
            .AsNoTracking()
            .Include(x => x.Profile)
            .Where(x => bidderIds.Contains(x.Id))
            .ToListAsync(cancellationToken);

        var bidderDisplayNames = bidderUsers.ToDictionary(
            x => x.Id.Value,
            x => AuctionNotificationDisplayNames.Resolve(x));

        var dtoItems = bidItems
            .Select(bid => bid.ToDto(bidderDisplayNames.TryGetValue(bid.BidderId.Value, out var dn) ? dn : null))
            .ToList();

        return dtoItems.ToPagedList(totalCount, parameters);
    }
}