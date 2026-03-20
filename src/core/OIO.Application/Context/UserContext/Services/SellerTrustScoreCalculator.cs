using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Data;
using OIO.Domain.Context.ModerationContext.Aggregates.Disputes;
using OIO.Domain.Context.ModerationContext.Enums;
using OIO.Domain.Context.OrderContext.Aggregates.Orders;
using OIO.Domain.Context.OrderContext.Enums;
using OIO.Domain.Context.ReviewContext.Aggregates;
using OIO.Domain.Context.UserContext.Aggregates.Users;
using OIO.Domain.Context.UserContext.Enums;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;

namespace OIO.Application.Context.UserContext.Services;

public sealed class SellerTrustScoreCalculator
{
    private const decimal DefaultScore = 50m;

    private const decimal RatingWeight = 0.30m;
    private const decimal CompletionWeight = 0.25m;
    private const decimal DisputeWeight = 0.20m;
    private const decimal VerificationWeight = 0.15m;
    private const decimal RiskWeight = 0.10m;

    private readonly IDbContext _dbContext;

    public SellerTrustScoreCalculator(IDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<decimal> CalculateAsync(UserId sellerId, CancellationToken ct)
    {
        // 1. Rating component (30%): AverageRating / 5.0 * 100
        var ratingSummary = await _dbContext.Set<SellerRatingSummary>()
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.SellerId == sellerId, ct);

        var ratingScore = ratingSummary is not null && ratingSummary.TotalReviews > 0
            ? ratingSummary.AverageRating / 5.0m * 100m
            : DefaultScore;

        // 2. Completion component (25%): CompletedOrders / TotalOrders * 100
        var totalOrders = await _dbContext.Set<Order>()
            .AsNoTracking()
            .CountAsync(o => o.SellerId == sellerId, ct);

        decimal completionScore;
        if (totalOrders == 0)
        {
            completionScore = DefaultScore;
        }
        else
        {
            var completedOrders = await _dbContext.Set<Order>()
                .AsNoTracking()
                .CountAsync(o => o.SellerId == sellerId && o.Status == OrderStatus.Completed, ct);

            completionScore = (decimal)completedOrders / totalOrders * 100m;
        }

        // 3. Dispute component (20%): (1 - OpenDisputes / TotalOrders) * 100
        decimal disputeScore;
        if (totalOrders == 0)
        {
            disputeScore = DefaultScore;
        }
        else
        {
            var openDisputes = await _dbContext.Set<Dispute>()
                .AsNoTracking()
                .CountAsync(d =>
                    d.RespondentId == sellerId &&
                    d.Status != DisputeStatus.Resolved &&
                    d.Status != DisputeStatus.Closed &&
                    d.Status != DisputeStatus.Cancelled, ct);

            disputeScore = (1m - (decimal)openDisputes / totalOrders) * 100m;
            if (disputeScore < 0m) disputeScore = 0m;
        }

        // 4. Verification component (15%): Approved → 100, otherwise AutoVerifyScore or 50
        var verification = await _dbContext.Set<IdentityVerification>()
            .AsNoTracking()
            .Where(v => v.UserId == sellerId)
            .OrderByDescending(v => v.CreatedAt)
            .FirstOrDefaultAsync(ct);

        decimal verificationScore;
        if (verification is not null && verification.Status == IdentityVerificationStatus.Approved)
        {
            verificationScore = 100m;
        }
        else if (verification?.AutoVerifyScore is not null)
        {
            verificationScore = verification.AutoVerifyScore.Value;
        }
        else
        {
            verificationScore = DefaultScore;
        }

        // 5. Risk component (10%): based on highest active risk flag severity
        var highestRiskFlag = await _dbContext.Set<UserRiskFlag>()
            .AsNoTracking()
            .Where(f => f.UserId == sellerId)
            .OrderByDescending(f => f.CreatedAt)
            .FirstOrDefaultAsync(ct);

        decimal riskScore;
        if (highestRiskFlag is null)
        {
            riskScore = 100m;
        }
        else if (highestRiskFlag.Severity == RiskFlagSeverity.Low)
        {
            riskScore = 80m;
        }
        else if (highestRiskFlag.Severity == RiskFlagSeverity.Medium)
        {
            riskScore = 50m;
        }
        else if (highestRiskFlag.Severity == RiskFlagSeverity.High)
        {
            riskScore = 20m;
        }
        else // Critical
        {
            riskScore = 0m;
        }

        var overall = ratingScore * RatingWeight
                    + completionScore * CompletionWeight
                    + disputeScore * DisputeWeight
                    + verificationScore * VerificationWeight
                    + riskScore * RiskWeight;

        return Math.Clamp(overall, 0m, 100m);
    }
}
