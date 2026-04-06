using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OIO.Application.Abstractions.Data;
using OIO.Application.Context.UserContext.Hubs;
using OIO.Application.Context.UserContext.Services;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions;
using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;

namespace OIO.Application.Context.AuctionContext.Services;

public sealed class AutoBidRealtimePublisher : IAutoBidRealtimePublisher
{
    private readonly IDbContext _dbContext;
    private readonly IUserNotificationService _userNotificationService;
    private readonly ILogger<AutoBidRealtimePublisher> _logger;

    public AutoBidRealtimePublisher(
        IDbContext dbContext,
        IUserNotificationService userNotificationService,
        ILogger<AutoBidRealtimePublisher> logger)
    {
        _dbContext = dbContext;
        _userNotificationService = userNotificationService;
        _logger = logger;
    }

    public async Task PublishAsync(Guid auctionId, Guid bidderId, CancellationToken ct = default)
    {
        try
        {
            var autoBid = await _dbContext.Set<AutoBid>()
                .AsNoTracking()
                .Include(ab => ab.Auction)
                .FirstOrDefaultAsync(
                    ab => ab.AuctionId == AuctionId.From(auctionId) &&
                          ab.BidderId == Domain.Context.UserContext.ValueObjects.Ids.UserId.From(bidderId),
                    ct);

            if (autoBid is null)
            {
                // Auto-bid was deleted or never existed — send terminal "removed" signal
                await _userNotificationService.NotifyAutoBidStateChangedAsync(
                    bidderId,
                    new AutoBidStateChangedNotification(
                        AuctionId: auctionId,
                        BidderId: bidderId,
                        IsEnabled: false,
                        MaxAmount: 0,
                        CurrentAmount: 0,
                        RemainingBudget: 0,
                        IncrementAmount: null,
                        Status: "removed",
                        Currency: "VND",
                        TotalAutoBids: 0,
                        LastAutoBidAt: null,
                        StopReason: "removed",
                        StoppedAt: DateTimeOffset.UtcNow,
                        LastValidationAt: null,
                        ServerTimestamp: DateTimeOffset.UtcNow),
                    ct);
                return;
            }

            var notification = BuildNotification(autoBid);
            await _userNotificationService.NotifyAutoBidStateChangedAsync(
                bidderId, notification, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to publish AutoBidStateChanged for auction {AuctionId}, bidder {BidderId}",
                auctionId, bidderId);
        }
    }

    public async Task PublishAllForAuctionAsync(Guid auctionId, CancellationToken ct = default)
    {
        try
        {
            var autoBids = await _dbContext.Set<AutoBid>()
                .AsNoTracking()
                .Include(ab => ab.Auction)
                .Where(ab => ab.AuctionId == AuctionId.From(auctionId))
                .ToListAsync(ct);

            foreach (var autoBid in autoBids)
            {
                try
                {
                    var notification = BuildNotification(autoBid);
                    await _userNotificationService.NotifyAutoBidStateChangedAsync(
                        autoBid.BidderId.Value, notification, ct);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Failed to publish AutoBidStateChanged for bidder {BidderId} on auction {AuctionId}",
                        autoBid.BidderId.Value, auctionId);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to publish AutoBidStateChanged for all bidders on auction {AuctionId}",
                auctionId);
        }
    }

    private static AutoBidStateChangedNotification BuildNotification(AutoBid autoBid)
    {
        var currency = autoBid.Budget.Currency.Id;

        return new AutoBidStateChangedNotification(
            AuctionId: autoBid.AuctionId.Value,
            BidderId: autoBid.BidderId.Value,
            IsEnabled: autoBid.IsEnabled,
            MaxAmount: autoBid.Budget.MaxAmount,
            CurrentAmount: autoBid.Budget.CurrentAmount,
            RemainingBudget: autoBid.Budget.Remaining.Amount,
            IncrementAmount: autoBid.Budget.IncrementAmount,
            Status: autoBid.Status.Id,
            Currency: currency,
            TotalAutoBids: autoBid.TotalAutoBids,
            LastAutoBidAt: autoBid.LastAutoBidAt.HasValue
                ? new DateTimeOffset(DateTime.SpecifyKind(autoBid.LastAutoBidAt.Value, DateTimeKind.Utc))
                : null,
            StopReason: autoBid.StopReason,
            StoppedAt: autoBid.StoppedAt.HasValue
                ? new DateTimeOffset(DateTime.SpecifyKind(autoBid.StoppedAt.Value, DateTimeKind.Utc))
                : null,
            LastValidationAt: autoBid.LastValidationAt.HasValue
                ? new DateTimeOffset(DateTime.SpecifyKind(autoBid.LastValidationAt.Value, DateTimeKind.Utc))
                : null,
            ServerTimestamp: DateTimeOffset.UtcNow);
    }
}
