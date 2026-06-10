using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.UserContext.Services;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions;
using OIO.Domain.Context.AuctionContext.Enums;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.AuctionContext.Queries.GetMyReservations;

/// <summary>
/// One reserved-funds line item for the current user. The sum of all items equals the
/// itemizable portion of the wallet's pending (reserved) balance; the front-end shows any
/// remaining unclassified holds (e.g. in-flight hybrid order payments) as a separate line.
/// </summary>
/// <param name="Type">"auction_deposit" | "auto_bid"</param>
/// <param name="ReferenceId">Auction id the hold belongs to.</param>
/// <param name="Title">Auction/item title for display.</param>
public sealed record ReservationItemDto(
    string Type,
    Guid ReferenceId,
    string? Title,
    decimal Amount,
    string Currency,
    string Status,
    DateTime CreatedAt);

public sealed record GetMyReservationsQuery() : IQuery<IReadOnlyList<ReservationItemDto>>;

internal sealed class GetMyReservationsQueryHandler
    : IQueryHandler<GetMyReservationsQuery, IReadOnlyList<ReservationItemDto>>
{
    private readonly IDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public GetMyReservationsQueryHandler(IDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async Task<Result<IReadOnlyList<ReservationItemDto>, Error>> Handle(
        GetMyReservationsQuery request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;

        // 1. Auction entry deposits currently held.
        var deposits = await _dbContext.Set<AuctionDeposit>()
            .AsNoTracking()
            .Where(d => d.BidderId == userId && d.Status == DepositStatus.Held)
            .Select(d => new ReservationItemDto(
                "auction_deposit",
                d.AuctionId.Value,
                d.Auction.Item.Title.Value,
                d.Amount.Amount,
                d.Amount.Currency.Id,
                d.Status.Id,
                d.CreatedAt))
            .ToListAsync(cancellationToken);

        // 2. Auto-bid reservations: wallet funds held for the configured max amount. The hold
        //    is only live while the auction is still running. The funds-release handler unholds
        //    the wallet when the auction ends but does NOT reset AutoBid.HeldAmount, so gate on
        //    the auction status (Active/Scheduled) to avoid reporting stale holds.
        var autoBidEntities = await _dbContext.Set<AutoBid>()
            .AsNoTracking()
            .Include(ab => ab.Auction)
                .ThenInclude(a => a.Item)
            .Where(ab => ab.BidderId == userId
                         && ab.HeldAmount > 0m
                         && (ab.Auction.Status == AuctionStatus.Active
                             || ab.Auction.Status == AuctionStatus.Scheduled))
            .ToListAsync(cancellationToken);

        var autoBids = autoBidEntities
            .Select(ab => new ReservationItemDto(
                "auto_bid",
                ab.AuctionId.Value,
                ab.Auction.Item.Title.Value,
                ab.HeldAmount,
                ab.Budget.Currency.Id,
                ab.Status.Id,
                ab.CreatedAt))
            .ToList();

        var items = deposits
            .Concat(autoBids)
            .OrderByDescending(x => x.CreatedAt)
            .ToList();

        return items;
    }
}
