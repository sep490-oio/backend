using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OIO.Application.Abstractions.Clock;
using OIO.Domain.Context.UserContext.Aggregates.Users;
using OIO.Infrastructure.Persistence;

namespace OIO.Infrastructure.BackgroundJobs.CleanUpJobs;

public sealed class ExpiredSessionCleanupJob : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ExpiredSessionCleanupJob> _logger;
    //TODO: bring interval to app settings
    private readonly TimeSpan _interval = TimeSpan.FromHours(6);
    
    public ExpiredSessionCleanupJob(
        IServiceScopeFactory scopeFactory,
        ILogger<ExpiredSessionCleanupJob> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Session cleanup job started. Interval: {Interval}", _interval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CleanupAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during token family cleanup.");
            }

            await Task.Delay(_interval, stoppingToken);
        }
    }

    private async Task CleanupAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var clock = scope.ServiceProvider.GetRequiredService<IClock>();

        var nowUtc = clock.UtcNow;

        // 1. Revoke families that passed absolute expiration
        var absoluteExpiredCount = await dbContext.Set<UserSession>()
            .Where(s => s.IsActive && s.AbsoluteExpiresAt <= nowUtc)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(f => f.IsActive, false)
                .SetProperty(f => f.RevokedAt, nowUtc)
                .SetProperty(f => f.RevokedReason, "Absolute expiration reached (cleanup job)"),
                ct);

        // 2. Revoke families that passed sliding expiration
        var slidingExpiredCount = await dbContext.Set<UserSession>()
            .Where(f => f.IsActive && f.ExpiresAt <= nowUtc)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(f => f.IsActive, false)
                .SetProperty(f => f.RevokedAt, nowUtc)
                .SetProperty(f => f.RevokedReason, "Sliding expiration reached (cleanup job)"),
                ct);

        // 3. Revoke orphaned tokens in revoked families
        var orphanedTokenCount = await dbContext.Set<UserRefreshToken>()
            .Where(t => t.RevokedAt == null)
            .Where(t => t.RefreshTokenFamily != null && !t.RefreshTokenFamily.IsActive)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(t => t.RevokedAt, nowUtc)
                .SetProperty(t => t.RevokedReason, "Family revoked (cleanup job)"),
                ct);

        // 4. Delete very old inactive families (older than 90 days)
        //TODO: bring number of day to app-settings
        var purgeThreshold = nowUtc.AddDays(-90);
        var purgedCount = await dbContext.Set<UserSession>()
            .Where(f => !f.IsActive && f.CreatedAt < purgeThreshold)
            .ExecuteDeleteAsync(ct);

        if (absoluteExpiredCount + slidingExpiredCount + orphanedTokenCount + purgedCount > 0)
        {
            _logger.LogInformation(
                "Token cleanup completed. " +
                "Absolute expired: {AbsoluteExpired}, " +
                "Sliding expired: {SlidingExpired}, " +
                "Orphaned tokens: {Orphaned}, " +
                "Purged old families: {Purged}",
                absoluteExpiredCount,
                slidingExpiredCount,
                orphanedTokenCount,
                purgedCount);
        }
    }

}