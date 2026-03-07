using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.AuctionContext.DTOs;
using OIO.Application.Context.UserContext.Services;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions;
using OIO.Domain.Context.AuctionContext.Errors;
using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.AuctionContext.Queries.GetMyAutoBid;

public sealed record GetMyAutoBidQuery(Guid AuctionId) : IQuery<AutoBidDto?>, IHasValidate
{
    public ViolationsError Validate()
    {
        return GetMyAutoBidQuery.Check()
            .WithOwnerName("GetMyAutoBid")
            .Field(AuctionId)
            .NotEmptyGuid();
    }
}

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
            .AnyAsync(a => a.Id == auctionId, cancellationToken);

        if (!auctionExists)
            return AuctionErrors.Auction.NotFound(auctionId);

        var autoBid = await _dbContext.Set<AutoBid>()
            .AsNoTracking()
            .Where(ab => ab.AuctionId == request.AuctionId &&
                         ab.BidderId == _currentUser.UserId)
            .Select(ab => new AutoBidDto(
                ab.Id.Value,
                ab.AuctionId.Value,
                ab.BidderId.Value,
                ab.IsEnabled,
                ab.MaxAmount.Amount,
                ab.CurrentAmount.Amount,
                ab.IncrementAmount != null ? ab.IncrementAmount.Amount : null,
                ab.Status.Id,
                ab.TotalAutoBids,
                ab.LastAutoBidAt,
                ab.CreatedAt))
            .FirstOrDefaultAsync(cancellationToken);

        return autoBid;
    }
}