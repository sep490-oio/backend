using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Context.AuctionContext.Commands.EndAuction;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions;
using OIO.Domain.Context.AuctionContext.Enums;
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
            var expiredReservationIds = auction.BuyNowReservations
                .Where(r => r.Status == BuyNowReservationStatus.PendingPayment && r.ExpiresAt <= nowUtc)
                .Select(r => r.Id)
                .ToList();

            foreach (var reservationId in expiredReservationIds)
            {
                var expireResult = auction.ExpireBuyNowReservation(reservationId, nowUtc);
                if (expireResult.IsFailure)
                {
                    _logger.LogWarning(
                        "Failed to expire buy-now reservation {ReservationId} for auction {AuctionId}: {Error}",
                        reservationId.Value,
                        auction.Id.Value,
                        expireResult.Error.Message);
                }
            }

            await dbContext.SaveChangesAsync(cancellationToken);

            if (auction.Status == AuctionStatus.Active &&
                auction.Info is not null &&
                auction.Info.EndTime <= nowUtc &&
                auction.GetActiveBuyNowReservation(nowUtc) is null)
            {
                await sender.Send(new EndAuctionCommand(auction.Id.Value), cancellationToken);
            }
        }
    }
}
