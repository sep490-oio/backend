using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OIO.Application.Abstractions.Data;
using OIO.Application.Context.UserContext.Hubs;
using OIO.Application.Context.UserContext.Services;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions;
using OIO.Domain.Context.AuctionContext.Enums;
using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;

namespace OIO.Application.Context.AuctionContext.Services;

public sealed class AuctionPositionPublisher : IAuctionPositionPublisher
{
    private readonly IDbContext _dbContext;
    private readonly IUserNotificationService _userNotificationService;
    private readonly ILogger<AuctionPositionPublisher> _logger;

    public AuctionPositionPublisher(
        IDbContext dbContext,
        IUserNotificationService userNotificationService,
        ILogger<AuctionPositionPublisher> logger)
    {
        _dbContext = dbContext;
        _userNotificationService = userNotificationService;
        _logger = logger;
    }

    public async Task PublishAsync(Guid auctionId, Guid bidderId, CancellationToken ct = default)
    {
        try
        {
            var auction = await LoadAuctionAsync(auctionId, ct);
            if (auction is null) return;

            var notification = BuildNotification(auction, UserId.From(bidderId));
            if (notification is null) return;

            await _userNotificationService.NotifyAuctionPositionChangedAsync(
                bidderId, notification, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to publish AuctionPositionChanged for auction {AuctionId}, bidder {BidderId}",
                auctionId, bidderId);
        }
    }

    public async Task PublishForAllBiddersAsync(Guid auctionId, CancellationToken ct = default)
    {
        try
        {
            var auction = await LoadAuctionAsync(auctionId, ct);
            if (auction is null) return;

            var bidderIds = auction.Bids
                .Select(b => b.BidderId)
                .Distinct()
                .ToList();

            foreach (var bidderId in bidderIds)
            {
                try
                {
                    var notification = BuildNotification(auction, bidderId);
                    if (notification is null) continue;

                    await _userNotificationService.NotifyAuctionPositionChangedAsync(
                        bidderId.Value, notification, ct);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Failed to publish AuctionPositionChanged for bidder {BidderId} on auction {AuctionId}",
                        bidderId.Value, auctionId);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to publish AuctionPositionChanged for all bidders on auction {AuctionId}",
                auctionId);
        }
    }

    private async Task<Auction?> LoadAuctionAsync(Guid auctionId, CancellationToken ct)
    {
        var auction = await _dbContext.GetByIdAsync<Auction, AuctionId>(
            AuctionId.From(auctionId),
            queryBuilder: query => query
                .AsNoTracking()
                .Include(a => a.Bids),
            cancellationToken: ct);

        if (auction is null)
        {
            _logger.LogWarning(
                "Skipped AuctionPositionChanged broadcast — auction {AuctionId} not found.",
                auctionId);
        }

        return auction;
    }

    private static AuctionPositionChangedNotification? BuildNotification(
        Auction auction, UserId bidderId)
    {
        var latestBid = auction.Bids
            .Where(b => b.BidderId == bidderId)
            .MaxBy(b => b.CreatedAt);

        if (latestBid is null)
            return null;

        var winningBid = auction.GetCurrentWinningBid();
        var isCurrentWinner = winningBid?.BidderId == bidderId;
        var auctionStatus = auction.Status.Id.ToLowerInvariant();

        string position;
        if (auctionStatus is "ended" or "sold")
            position = auction.WinnerId == bidderId ? "won" : "lost";
        else if (auctionStatus is "failed" or "cancelled" or "terminated")
            position = "lost";
        else if (isCurrentWinner)
            position = "leading";
        else
            position = "outbid";

        return new AuctionPositionChangedNotification(
            AuctionId: auction.Id.Value,
            Position: position,
            IsCurrentWinner: isCurrentWinner,
            CurrentPrice: auction.Pricing.CurrentAmount,
            MinimumNextBid: isCurrentWinner ? 0 : auction.GetMinimumBidAmount().Amount,
            Currency: auction.Pricing.Currency.Id,
            LatestBidId: latestBid.Id.Value,
            LatestBidAmount: latestBid.Amount.Amount,
            LatestBidStatus: latestBid.Status.Id,
            Timestamp: DateTimeOffset.UtcNow);
    }
}
