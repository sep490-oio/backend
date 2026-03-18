using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.AuctionContext.DTOs;
using OIO.Application.Context.AuctionContext.Mappings;
using OIO.Application.Context.UserContext.Services;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions;
using OIO.Domain.Context.AuctionContext.Errors;
using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.AuctionContext.Queries.GetMyAutoBid;

internal sealed class GetMyAutoBidQueryHandler
    : IQueryHandler<GetMyAutoBidQuery, AutoBidDto?>
{
    private readonly IDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public GetMyAutoBidQueryHandler(
        IDbContext dbContext,
        ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async Task<Result<AutoBidDto?, Error>> Handle(
        GetMyAutoBidQuery request,
        CancellationToken cancellationToken)
    {
        var auctionId = AuctionId.From(request.AuctionId);
        var auctionExists = await _dbContext.Set<Auction>()
            .AsNoTracking()
            .AnyAsync(a => a.Id == auctionId, cancellationToken);

        if (!auctionExists)
            return AuctionErrors.Auction.NotFound(auctionId);

        var autoBid = await _dbContext.Set<AutoBid>()
            .AsNoTracking()
            .Where(ab => ab.AuctionId == auctionId &&
                         ab.BidderId == _currentUser.UserId)
            .FirstOrDefaultAsync(cancellationToken);

        return autoBid?.ToDto();
    }
}
