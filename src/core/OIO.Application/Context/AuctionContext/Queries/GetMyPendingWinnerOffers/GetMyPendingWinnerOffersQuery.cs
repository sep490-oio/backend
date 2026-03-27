using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.AuctionContext.DTOs;
using OIO.Application.Context.UserContext.Services;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions;
using OIO.Domain.Context.AuctionContext.Enums;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.AuctionContext.Queries.GetMyPendingWinnerOffers;

public sealed record GetMyPendingWinnerOffersQuery : IQuery<List<WinnerOfferDto>>;

internal sealed class GetMyPendingWinnerOffersQueryHandler
    : IQueryHandler<GetMyPendingWinnerOffersQuery, List<WinnerOfferDto>>
{
    private readonly IDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public GetMyPendingWinnerOffersQueryHandler(
        IDbContext dbContext,
        ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async Task<Result<List<WinnerOfferDto>, Error>> Handle(
        GetMyPendingWinnerOffersQuery request,
        CancellationToken cancellationToken)
    {
        var offers = await _dbContext.Set<AuctionWinnerOffer>()
            .AsNoTracking()
            .Include(o => o.Auction)
                .ThenInclude(a => a.Item)
            .Where(o => o.UserId == _currentUser.UserId
                     && o.OfferStatus == WinnerOfferStatus.Pending)
            .OrderByDescending(o => o.OfferedAt)
            .Select(o => new WinnerOfferDto(
                o.Id.Value,
                o.AuctionId.Value,
                o.Auction.Item.Title.Value,
                o.Auction.Pricing.CurrentAmount,
                o.Auction.Pricing.Currency.Id,
                o.OfferStatus.Id,
                o.ExpiresAt,
                o.OfferedAt))
            .ToListAsync(cancellationToken);

        return offers;
    }
}
