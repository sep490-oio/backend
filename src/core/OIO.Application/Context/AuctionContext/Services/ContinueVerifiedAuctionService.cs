using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Data;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions;
using OIO.Domain.Context.AuctionContext.Enums;
using OIO.Domain.SeedWork.Errors;
using ItemId = OIO.Domain.Context.CatalogContext.ValueObjects.Ids.ItemId;

namespace OIO.Application.Context.AuctionContext.Services;

public sealed record VerifiedAuctionContinuationResult(
    Guid? AuctionId,
    string? AuctionStatus,
    bool Continued);

internal sealed class ContinueVerifiedAuctionService
{
    private readonly IDbContext _dbContext;
    private readonly IClock _clock;

    public ContinueVerifiedAuctionService(IDbContext dbContext, IClock clock)
    {
        _dbContext = dbContext;
        _clock = clock;
    }

    public async Task<Result<VerifiedAuctionContinuationResult, Error>> ContinueAsync(
        Guid itemIdValue,
        CancellationToken cancellationToken)
    {
        var itemId = ItemId.From(itemIdValue);
        var auction = await _dbContext.Set<Auction>()
            .Include(x => x.Item)
            .Where(x => x.ItemId == itemId)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (auction is null)
            return Result.Success<VerifiedAuctionContinuationResult, Error>(
                new VerifiedAuctionContinuationResult(null, null, false));

        var nowUtc = _clock.UtcNow;
        var previousStatus = auction.Status;

        if (auction.Status == AuctionStatus.Draft)
        {
            var submitResult = auction.SubmitConfiguration(nowUtc);
            if (submitResult.IsFailure)
                return submitResult.Error;
        }
        else if (auction.Status == AuctionStatus.Approved && auction.Info is not null)
        {
            var timingResult = auction.SetTiming(auction.Info, nowUtc);
            if (timingResult.IsFailure)
                return timingResult.Error;
        }

        return Result.Success<VerifiedAuctionContinuationResult, Error>(
            new VerifiedAuctionContinuationResult(
                auction.Id.Value,
                auction.Status.Id,
                previousStatus != auction.Status));
    }
}
