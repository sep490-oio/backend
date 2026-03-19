using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OIO.Application.Abstractions.Data;
using OIO.Application.Context.AuctionContext.Commands.ReturnAuctionDeposit;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions;
using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;

namespace OIO.Application.Context.AuctionContext;

internal static class AuctionDepositReleaseDispatch
{
    public static async Task<IReadOnlyList<Guid>> ReturnHeldDepositsAsync(
        IDbContext dbContext,
        ISender sender,
        ILogger logger,
        Guid auctionId,
        string reason,
        CancellationToken cancellationToken,
        Guid? excludedBidderId = null)
    {
        var parsedAuctionId = AuctionId.From(auctionId);

        var releaseCandidates = await dbContext.Set<AuctionDeposit>()
            .AsNoTracking()
            .Where(d => d.AuctionId == parsedAuctionId && d.ReleasedAt == null)
            .Select(d => new AuctionDepositReleaseCandidate(d.Id.Value, d.BidderId.Value))
            .ToListAsync(cancellationToken);

        if (releaseCandidates.Count == 0)
        {
            logger.LogDebug(
                "No held auction deposits found to release for auction {AuctionId}.",
                auctionId);
            return [];
        }

        var releasedBidderIds = new List<Guid>();

        foreach (var candidate in releaseCandidates)
        {
            if (excludedBidderId.HasValue && candidate.BidderId == excludedBidderId.Value)
                continue;

            var result = await sender.Send(
                new ReturnAuctionDepositCommand(candidate.DepositId, reason),
                cancellationToken);

            if (result.IsFailure)
            {
                logger.LogWarning(
                    "Failed to return deposit {DepositId} for auction {AuctionId}. Error={Error}",
                    candidate.DepositId,
                    auctionId,
                    result.Error.Message);
            }
            else
            {
                releasedBidderIds.Add(candidate.BidderId);
            }
        }

        return releasedBidderIds.Distinct().ToList();
    }

    private sealed record AuctionDepositReleaseCandidate(Guid DepositId, Guid BidderId);
}
