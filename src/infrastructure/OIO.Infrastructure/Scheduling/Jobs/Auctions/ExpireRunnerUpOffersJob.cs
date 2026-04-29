using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Context.NotificationContext.Commands.CreateNotification;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions;
using OIO.Domain.Context.AuctionContext.Enums;
using OIO.Domain.Context.NotificationContext.Enums;
using OIO.Infrastructure.Persistence;

namespace OIO.Infrastructure.Scheduling.Jobs.Auctions;

public sealed class ExpireRunnerUpOffersJob : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ExpireRunnerUpOffersJob> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromMinutes(1);

    public ExpireRunnerUpOffersJob(
        IServiceScopeFactory scopeFactory,
        ILogger<ExpireRunnerUpOffersJob> logger)
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
                await ExpireOffersAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while expiring runner-up offers.");
            }

            await Task.Delay(_interval, stoppingToken);
        }
    }

    private async Task ExpireOffersAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        var clock = scope.ServiceProvider.GetRequiredService<IClock>();
        var nowUtc = clock.UtcNow;

        var auctions = await dbContext.Set<Auction>()
            .Include(x => x.Item)
            .Include(x => x.WinnerOffers)
            .Where(x =>
                x.Status == AuctionStatus.PaymentDefaulted &&
                x.WinnerOffers.Any(o =>
                    o.OfferStatus == WinnerOfferStatus.Pending &&
                    o.ExpiresAt != null &&
                    o.ExpiresAt < nowUtc))
            .Take(50)
            .ToListAsync(cancellationToken);

        foreach (var auction in auctions)
        {
            var expiredOffers = auction.WinnerOffers
                .Where(o =>
                    o.OfferStatus == WinnerOfferStatus.Pending &&
                    o.ExpiresAt != null &&
                    o.ExpiresAt < nowUtc)
                .Select(o => o.UserId.Value)
                .ToList();

            if (!auction.ExpirePendingWinnerOffers(nowUtc))
                continue;

            await dbContext.SaveChangesAsync(cancellationToken);

            foreach (var bidderId in expiredOffers)
            {
                var bidderNotify = await sender.Send(
                    new CreateNotificationCommand(
                        UserId: bidderId,
                        NotificationType: "auction",
                        EventType: "runner_up_offer_expired",
                        Title: "Loi moi runner-up da het han",
                        Message: $"Loi moi nhan quyen mua cho phien dau gia \"{auction.Item.Title.Value}\" da het han.",
                        Priority: NotificationPriority.Normal,
                        EntityType: "Auction",
                        EntityId: auction.Id.Value),
                    cancellationToken);

                if (bidderNotify.IsFailure)
                {
                    _logger.LogWarning(
                        "Failed to notify expired runner-up bidder {BidderId} for auction {AuctionId}: {Error}",
                        bidderId,
                        auction.Id.Value,
                        bidderNotify.Error.Message);
                }
            }

            var sellerNotify = await sender.Send(
                new CreateNotificationCommand(
                    UserId: auction.Item.SellerId.Value,
                    NotificationType: "auction",
                    EventType: "runner_up_offer_expired",
                    Title: "Runner-up khong phan hoi dung han",
                    Message: $"Loi moi runner-up cua phien dau gia \"{auction.Item.Title.Value}\" da het han. Ban co the moi nguoi tiep theo hoac relist.",
                    Priority: NotificationPriority.High,
                    EntityType: "Auction",
                    EntityId: auction.Id.Value,
                    Actions: System.Text.Json.JsonSerializer.Serialize(new[]
                    {
                        new
                        {
                            type = "offer_next_rank",
                            label = "Moi runner-up",
                            method = "POST",
                            endpoint = $"api/auctions/{auction.Id.Value}/runner-up-offers"
                        },
                        new
                        {
                            type = "relist",
                            label = "Relist",
                            method = "POST",
                            endpoint = $"api/auctions/{auction.Id.Value}/relist"
                        }
                    })),
                cancellationToken);

            if (sellerNotify.IsFailure)
            {
                _logger.LogWarning(
                    "Failed to notify seller {SellerId} for expired runner-up offers on auction {AuctionId}: {Error}",
                    auction.Item.SellerId.Value,
                    auction.Id.Value,
                    sellerNotify.Error.Message);
            }
        }
    }
}
