using MediatR;
using Microsoft.Extensions.Logging;
using OIO.Application.Abstractions.Scheduling;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions.Events;

namespace OIO.Application.Context.AuctionContext.EventHandlers;

/// <summary>
/// When a buy-now reservation expires or fails, <see cref="Auction.ApplyBuyNowCompensation"/>
/// extends the auction timing to compensate for the locked period. This handler reschedules
/// the Quartz timers so <c>CloseQualificationJob</c>, <c>ActivateAuctionJob</c>, and
/// <c>EndAuctionJob</c> fire at the correct (extended) times.
///
/// Without this, the one-shot qualification close timer fires at the original time and the
/// auction is incorrectly cancelled or stuck in Scheduled status.
/// </summary>
internal sealed class BuyNowCompensationTimerRescheduleHandler
    : INotificationHandler<AuctionBuyNowCompensationExtendedEvent>
{
    private readonly IAuctionScheduler _scheduler;
    private readonly ILogger<BuyNowCompensationTimerRescheduleHandler> _logger;

    public BuyNowCompensationTimerRescheduleHandler(
        IAuctionScheduler scheduler,
        ILogger<BuyNowCompensationTimerRescheduleHandler> logger)
    {
        _scheduler = scheduler;
        _logger = logger;
    }

    public async Task Handle(
        AuctionBuyNowCompensationExtendedEvent notification,
        CancellationToken ct)
    {
        var auctionId = Guid.Parse(notification.AuctionId);

        if (notification.AuctionPhase == "deposit")
        {
            // Deposit phase: qualification close, start time, and end time were all extended.
            if (notification.NewQualificationEndTime.HasValue)
            {
                await _scheduler.ScheduleQualificationCloseAsync(
                    auctionId, notification.NewQualificationEndTime.Value, ct);
            }

            await _scheduler.ScheduleStartAsync(
                auctionId, notification.NewStartTime, ct);
            await _scheduler.ScheduleEndAsync(
                auctionId, notification.NewEndTime, ct);

            _logger.LogInformation(
                "Rescheduled all timers after buy-now compensation (deposit phase). " +
                "Auction={AuctionId}, QualClose={QualClose}, Start={Start}, End={End}, Compensation={Duration}",
                notification.AuctionId,
                notification.NewQualificationEndTime,
                notification.NewStartTime,
                notification.NewEndTime,
                notification.CompensationDuration);
        }
        else
        {
            // Active phase: only end time was extended.
            await _scheduler.RescheduleEndAsync(
                auctionId, notification.NewEndTime, ct);

            _logger.LogInformation(
                "Rescheduled end timer after buy-now compensation (active phase). " +
                "Auction={AuctionId}, NewEnd={NewEnd}, Compensation={Duration}",
                notification.AuctionId,
                notification.NewEndTime,
                notification.CompensationDuration);
        }
    }
}
