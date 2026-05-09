using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Scheduling;
using OIO.Application.Context.AuctionContext.Commands.EndAuction;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions;
using OIO.Domain.Context.AuctionContext.Enums;
using OIO.Domain.Context.OrderContext.Aggregates.Orders;
using OIO.Domain.Context.OrderContext.Enums;
using OIO.Infrastructure.Persistence;

namespace OIO.Infrastructure.Scheduling.Jobs.Auctions;

public sealed class ExpireBuyNowReservationsJob : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ExpireBuyNowReservationsJob> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromMinutes(1);

    public ExpireBuyNowReservationsJob(
        IServiceScopeFactory scopeFactory,
        ILogger<ExpireBuyNowReservationsJob> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ExpireReservationsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while expiring buy-now reservations.");
            }

            await Task.Delay(_interval, stoppingToken);
        }
    }

    private async Task ExpireReservationsAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var clock = scope.ServiceProvider.GetRequiredService<IClock>();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        var nowUtc = clock.UtcNow;

        var auctions = await dbContext.Set<Auction>()
            .Include(x => x.BuyNowReservations)
            .Where(x => x.BuyNowReservations.Any(r =>
                r.Status == BuyNowReservationStatus.PendingPayment &&
                r.ExpiresAt <= nowUtc))
            .Take(50)
            .ToListAsync(cancellationToken);

        foreach (var auction in auctions)
        {
            var expiringReservations = auction.BuyNowReservations
                .Where(r => r.Status == BuyNowReservationStatus.PendingPayment && r.ExpiresAt <= nowUtc)
                .Select(r => new { r.Id, r.OrderId })
                .ToList();

            var linkedOrderIds = expiringReservations
                .Where(r => r.OrderId is not null)
                .Select(r => r.OrderId!)
                .ToList();

            var linkedOrders = linkedOrderIds.Count == 0
                ? new List<Order>()
                : await dbContext.Set<Order>()
                    .Where(o => linkedOrderIds.Contains(o.Id))
                    .ToListAsync(cancellationToken);

            foreach (var reservation in expiringReservations)
            {
                var expireResult = auction.ExpireBuyNowReservation(reservation.Id, nowUtc);
                if (expireResult.IsFailure)
                {
                    _logger.LogWarning(
                        "Failed to expire buy-now reservation {ReservationId} for auction {AuctionId}: {Error}",
                        reservation.Id.Value,
                        auction.Id.Value,
                        expireResult.Error.Message);
                    continue;
                }

                if (reservation.OrderId is null)
                    continue;

                var linkedOrder = linkedOrders.FirstOrDefault(o => o.Id == reservation.OrderId);
                if (linkedOrder is null || linkedOrder.Status != OrderStatus.PendingPayment)
                    continue;

                var cancelResult = linkedOrder.Cancel("buy_now_reservation_expired", nowUtc);
                if (cancelResult.IsFailure)
                {
                    _logger.LogWarning(
                        "Failed to cancel order {OrderId} linked to expired buy-now reservation {ReservationId}: {Error}",
                        linkedOrder.Id.Value,
                        reservation.Id.Value,
                        cancelResult.Error.Message);
                }
            }

            await dbContext.SaveChangesAsync(cancellationToken);

            // After compensation extends auction timings, reschedule Quartz timers
            // so CloseQualificationJob / ActivateAuctionJob / EndAuctionJob fire at
            // the correct (extended) times. Without this, the one-shot qualification
            // close timer fires at the original time and the auction is incorrectly
            // cancelled or gets stuck in Scheduled status forever.
            if (auction.Info is not null)
            {
                var scheduler = scope.ServiceProvider.GetRequiredService<IAuctionScheduler>();

                if (auction.Status == AuctionStatus.Scheduled && auction.Info.HasQualification)
                {
                    // Deposit phase: reschedule qualification close, start, and end
                    await scheduler.ScheduleQualificationCloseAsync(
                        auction.Id.Value, auction.Info.Qualification!.EndTime, cancellationToken);
                    await scheduler.ScheduleStartAsync(
                        auction.Id.Value, auction.Info.StartTime, cancellationToken);
                    await scheduler.ScheduleEndAsync(
                        auction.Id.Value, auction.Info.EndTime, cancellationToken);

                    _logger.LogInformation(
                        "Rescheduled timers for auction {AuctionId} after buy-now compensation. " +
                        "QualClose={QualEndTime}, Start={StartTime}, End={EndTime}",
                        auction.Id.Value, auction.Info.Qualification.EndTime,
                        auction.Info.StartTime, auction.Info.EndTime);
                }
                else if (auction.Status == AuctionStatus.Active)
                {
                    // Active phase: only end time was extended
                    await scheduler.RescheduleEndAsync(
                        auction.Id.Value, auction.Info.EndTime, cancellationToken);

                    // Check if auction should end now
                    if (auction.Info.EndTime <= nowUtc
                        && auction.GetActiveBuyNowReservation(nowUtc) is null)
                    {
                        await sender.Send(new EndAuctionCommand(auction.Id.Value), cancellationToken);
                    }
                }
            }
        }
    }
}
