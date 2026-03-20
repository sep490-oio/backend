using Microsoft.EntityFrameworkCore;
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
    private readonly IDbContext _dbContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly SellerTrustScoreCalculator _calculator;
    private readonly ILogger<RecalculateSellerTrustScoresJob> _logger;

    public RecalculateSellerTrustScoresJob(
        IDbContext dbContext,
        IUnitOfWork unitOfWork,
        IClock clock,
        SellerTrustScoreCalculator calculator,
        ILogger<RecalculateSellerTrustScoresJob> logger)
    {
        _dbContext = dbContext;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _calculator = calculator;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        var now = _clock.UtcNow;

        _logger.LogInformation("Running RecalculateSellerTrustScoresJob at {Now}.", now);

        var sellers = await _dbContext.Set<SellerProfile>()
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
                var score = await _calculator.CalculateAsync(seller.Id, context.CancellationToken);
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
            await _unitOfWork.SaveChangesAsync(context.CancellationToken);
        }

        _logger.LogInformation(
            "RecalculateSellerTrustScoresJob finished. Updated {Count}/{Total} sellers.",
            updatedCount, sellers.Count);
    }
}
