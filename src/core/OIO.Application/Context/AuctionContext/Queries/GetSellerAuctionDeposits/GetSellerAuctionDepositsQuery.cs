using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.UserContext.Services;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.AuctionContext.Queries.GetSellerAuctionDeposits;

// ── DTOs ────────────────────────────────────────────────────────────────

/// <summary>
/// Per-auction deposit summary row shown on the seller wallet dashboard.
/// </summary>
public sealed record SellerAuctionDepositRowDto(
    Guid AuctionId,
    string ItemTitle,
    string AuctionStatus,
    int TotalDeposits,
    int ActiveDeposits,
    decimal TotalHeldAmount,
    string Currency,
    DateTime? AuctionEndTime,
    IReadOnlyList<DepositDetailDto> Deposits);

public sealed record DepositDetailDto(
    Guid DepositId,
    Guid BidderId,
    string BidderDisplayName,
    decimal Amount,
    string Currency,
    string Status,
    DateTime CreatedAt,
    DateTime? ReleasedAt);

// ── Query ───────────────────────────────────────────────────────────────

public sealed record GetSellerAuctionDepositsQuery
    : IQuery<IReadOnlyList<SellerAuctionDepositRowDto>>;

// ── Handler ─────────────────────────────────────────────────────────────

internal sealed class GetSellerAuctionDepositsQueryHandler(
    IDbContext dbContext,
    ICurrentUser currentUser)
    : IQueryHandler<GetSellerAuctionDepositsQuery, IReadOnlyList<SellerAuctionDepositRowDto>>
{
    public async Task<Result<IReadOnlyList<SellerAuctionDepositRowDto>, Error>> Handle(
        GetSellerAuctionDepositsQuery request,
        CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;

        // 1. Load all auctions owned by this seller that have deposits,
        //    together with their deposit + bidder info.
        var auctions = await dbContext.Set<Auction>()
            .AsNoTracking()
            .Include(a => a.Item)
            .Include(a => a.Deposits)
                .ThenInclude(d => d.Bidder)
            .Where(a => a.Item.SellerId == userId && a.Deposits.Any())
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync(cancellationToken);

        if (auctions.Count == 0)
            return Result.Success<IReadOnlyList<SellerAuctionDepositRowDto>, Error>(
                Array.Empty<SellerAuctionDepositRowDto>());

        // 2. Project to DTOs. Item is already loaded via Include.
        var rows = auctions.Select(auction =>
        {
            var deposits = auction.Deposits.OrderByDescending(d => d.CreatedAt).ToList();
            var activeDeposits = deposits.Where(d => d.IsHeld).ToList();

            return new SellerAuctionDepositRowDto(
                AuctionId: auction.Id.Value,
                ItemTitle: auction.Item?.Title?.Value ?? string.Empty,
                AuctionStatus: auction.Status.Id,
                TotalDeposits: deposits.Count,
                ActiveDeposits: activeDeposits.Count,
                TotalHeldAmount: activeDeposits.Sum(d => d.Amount.Amount),
                Currency: activeDeposits.FirstOrDefault()?.Amount.Currency.Id
                          ?? deposits.FirstOrDefault()?.Amount.Currency.Id
                          ?? "VND",
                AuctionEndTime: auction.Info?.EndTime,
                Deposits: deposits.Select(d => new DepositDetailDto(
                    DepositId: d.Id.Value,
                    BidderId: d.BidderId.Value,
                    BidderDisplayName: d.Bidder?.UserName?.Value ?? d.BidderId.Value.ToString()[..8],
                    Amount: d.Amount.Amount,
                    Currency: d.Amount.Currency.Id,
                    Status: d.Status.Id,
                    CreatedAt: d.CreatedAt,
                    ReleasedAt: d.ReleasedAt
                )).ToList()
            );
        }).ToList();

        return Result.Success<IReadOnlyList<SellerAuctionDepositRowDto>, Error>(rows);
    }
}
