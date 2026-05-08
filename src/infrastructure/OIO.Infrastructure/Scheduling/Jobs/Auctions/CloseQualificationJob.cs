using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Scheduling;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions;
using OIO.Domain.Context.AuctionContext.Enums;
using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;
using OIO.Domain.Context.CatalogContext.Aggregates.Items;
using OIO.Domain.Context.CatalogContext.ValueObjects.Ids;
using Quartz;

namespace OIO.Infrastructure.Scheduling.Jobs.Auctions;

/// <summary>
/// Fires at exactly Qualification.EndTime for a given auction.
/// If fewer than 2 bid-eligible participants (qualified + held deposit) are present,
/// the auction is auto-cancelled immediately — no need to wait until ActivateAuctionJob.
/// </summary>
[DisallowConcurrentExecution]
public sealed class CloseQualificationJob : IJob
{
    internal const string AutoCancelReason =
        "Auction automatically cancelled because fewer than 2 participants deposited during the qualification window.";

    public static JobKey BuildJobKey(Guid auctionId) =>
        new($"close-qualification-{auctionId}", JobConstants.AuctionLifecycleGroup);

    public static TriggerKey BuildTriggerKey(Guid auctionId) =>
        new($"close-qualification-trigger-{auctionId}", JobConstants.AuctionLifecycleGroup);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IClock _clock;
    private readonly ILogger<CloseQualificationJob> _logger;

    public CloseQualificationJob(
        IServiceScopeFactory scopeFactory,
        IClock clock,
        ILogger<CloseQualificationJob> logger)
    {
        _scopeFactory = scopeFactory;
        _clock = clock;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        var auctionId = context.MergedJobDataMap.GetGuidValue(nameof(AuctionId));
        var nowUtc = _clock.UtcNow;

        _logger.LogInformation(
            "⏰ CloseQualificationJob fired for auction {AuctionId}.", auctionId);

        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IDbContext>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var scheduler = scope.ServiceProvider.GetRequiredService<IAuctionScheduler>();

        var parsedId = AuctionId.From(auctionId);
        var auction = await dbContext.GetByIdAsync<Auction, AuctionId>(
            parsedId,
            query => query
                .Include(a => a.Deposits)
                .Include(a => a.Participants)
                .Include(a => a.Item)
                .AsSplitQuery(),
            context.CancellationToken);

        if (auction is null)
        {
            _logger.LogWarning(
                "CloseQualificationJob: auction {AuctionId} not found.", auctionId);
            return;
        }

        // Idempotent: only process Scheduled auctions.
        if (auction.Status != AuctionStatus.Scheduled)
        {
            _logger.LogDebug(
                "CloseQualificationJob: auction {AuctionId} is {Status}, skipping.",
                auctionId, auction.Status.Id);
            return;
        }

        // Check if the qualification window has actually closed.
        if (auction.Info?.Qualification is null || !auction.Info.Qualification.HasClosed(nowUtc))
        {
            _logger.LogDebug(
                "CloseQualificationJob: qualification window for auction {AuctionId} is not yet closed, skipping.",
                auctionId);
            return;
        }

        // Core logic: fewer than 2 bid-eligible participants → auto-cancel.
        if (auction.HasBidEligibleParticipants(nowUtc))
        {
            _logger.LogInformation(
                "✅ CloseQualificationJob: auction {AuctionId} has >= 2 eligible participants. Proceeding to activation phase.",
                auctionId);
            return;
        }

        // Count held deposits for logging.
        var heldDepositCount = auction.Deposits.Count(d => d.IsHeld);

        _logger.LogWarning(
            "❌ CloseQualificationJob: auction {AuctionId} has only {DepositCount} held deposit(s). Auto-cancelling.",
            auctionId, heldDepositCount);

        var cancelResult = auction.CancelAuction(AutoCancelReason, nowUtc);
        if (cancelResult.IsFailure)
        {
            _logger.LogError(
                "CloseQualificationJob: cancel failed for auction {AuctionId}: {Error}.",
                auctionId, cancelResult.Error.Message);
            return;
        }

        // Return item to active state so seller can relist.
        var item = await dbContext.GetByIdAsync<Item, ItemId>(
            auction.ItemId, cancellationToken: context.CancellationToken);

        if (item is not null)
        {
            var returnResult = item.ReturnToActive(nowUtc);
            if (returnResult.IsFailure)
            {
                _logger.LogWarning(
                    "CloseQualificationJob: ReturnToActive failed for item {ItemId}: {Error}.",
                    auction.ItemId, returnResult.Error.Message);
            }
        }

        await unitOfWork.SaveChangesAsync(context.CancellationToken);

        // Cancel downstream scheduled jobs (start, end) since auction is cancelled.
        await scheduler.CancelAsync(auctionId, context.CancellationToken);

        _logger.LogWarning(
            "🛑 CloseQualificationJob: auction {AuctionId} auto-cancelled. Deposits: {DepositCount}. Reason: {Reason}",
            auctionId, heldDepositCount, AutoCancelReason);
    }
}
