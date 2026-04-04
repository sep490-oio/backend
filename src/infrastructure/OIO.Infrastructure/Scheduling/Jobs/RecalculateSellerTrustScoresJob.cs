using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Data;
using OIO.Application.Context.UserContext.Services;
using OIO.Domain.Context.UserContext.Aggregates.Users;
using OIO.Domain.Context.UserContext.Enums;
using Quartz;

namespace OIO.Infrastructure.Scheduling.Jobs;

/// <summary>
/// Recalculates trust scores for all verified sellers every 6 hours.
/// </summary>
[DisallowConcurrentExecution]
public sealed class RecalculateSellerTrustScoresJob : IJob
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IClock _clock;
    private readonly ILogger<RecalculateSellerTrustScoresJob> _logger;

    public RecalculateSellerTrustScoresJob(
        IServiceScopeFactory scopeFactory,
        IClock clock,
        ILogger<RecalculateSellerTrustScoresJob> logger)
    {
        _scopeFactory = scopeFactory;
        _clock = clock;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IDbContext>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var calculator = scope.ServiceProvider.GetRequiredService<SellerTrustScoreCalculator>();

        var now = _clock.UtcNow;

        _logger.LogInformation("Running RecalculateSellerTrustScoresJob at {Now}.", now);

        var sellers = await dbContext.Set<SellerProfile>()
            .Where(s => s.Status == SellerProfileStatus.Verified)
            .ToListAsync(context.CancellationToken);

        if (sellers.Count == 0)
        {
            _logger.LogDebug("No verified sellers found for trust score recalculation.");
            return;
        }

        var updatedCount = 0;

        foreach (var seller in sellers)
        {
            try
            {
                var score = await calculator.CalculateAsync(seller.Id, context.CancellationToken);
                seller.UpdateTrustScore(score, now);
                updatedCount++;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to calculate trust score for seller {SellerId}.", seller.Id);
            }
        }

        if (updatedCount > 0)
        {
            await unitOfWork.SaveChangesAsync(context.CancellationToken);
        }

        _logger.LogInformation(
            "RecalculateSellerTrustScoresJob finished. Updated {Count}/{Total} sellers.",
            updatedCount, sellers.Count);
    }
}
