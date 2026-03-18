using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OIO.Application.Context.AuctionContext.Services;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions;
using OIO.Domain.Context.AuctionContext.Enums;
using OIO.Infrastructure.Persistence;

namespace OIO.Infrastructure.Scheduling.Jobs.Auctions;

public sealed class ScanActiveAuctionsForCollusionJob : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ScanActiveAuctionsForCollusionJob> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromMinutes(5);

    public ScanActiveAuctionsForCollusionJob(
        IServiceScopeFactory scopeFactory,
        ILogger<ScanActiveAuctionsForCollusionJob> logger)
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
                await ScanAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while scanning auctions for collusion signals.");
            }

            await Task.Delay(_interval, stoppingToken);
        }
    }

    private async Task ScanAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var detectionService = scope.ServiceProvider.GetRequiredService<IAuctionCollusionDetectionService>();

        var auctionIds = await dbContext.Set<Auction>()
            .AsNoTracking()
            .Where(x => x.Status == AuctionStatus.Active || x.Status == AuctionStatus.Scheduled)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => x.Id.Value)
            .Take(100)
            .ToListAsync(cancellationToken);

        foreach (var auctionId in auctionIds)
        {
            await detectionService.ScanAuctionAsync(auctionId, cancellationToken);
        }
    }
}
