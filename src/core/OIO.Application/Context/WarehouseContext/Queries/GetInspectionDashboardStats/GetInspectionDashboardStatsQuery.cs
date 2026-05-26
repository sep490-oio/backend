using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Domain.Context.WarehouseContext.Aggregates.WarehouseItems;
using OIO.Domain.Context.WarehouseContext.Enums;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.WarehouseContext.Queries.GetInspectionDashboardStats;

public sealed record InspectionDashboardStatsDto(
    int AwaitingInspection,
    int AwaitingReview,
    int TodayCompleted);

public sealed record GetInspectionDashboardStatsQuery() : IQuery<InspectionDashboardStatsDto>;

internal sealed class GetInspectionDashboardStatsQueryHandler(IDbContext db, IClock clock)
    : IQueryHandler<GetInspectionDashboardStatsQuery, InspectionDashboardStatsDto>
{
    public async Task<Result<InspectionDashboardStatsDto, Error>> Handle(
        GetInspectionDashboardStatsQuery request,
        CancellationToken cancellationToken)
    {
        var nowUtc = clock.UtcNow;
        var startOfDay = nowUtc.Date; // assuming UtcNow is what we use for "today" in dashboard

        // 1. Pending Inspection: WarehouseItems that are Stored (or Inspected but no active review? 
        // Actually, looking at GetInspectionQueueQuery: 
        // Pending Inspection = Stored OR Inspected, AND NOT (DecisionStatus == PendingReview)
        // Wait, the queue says: if inspection is null => AwaitingInspection.
        // Let's count Stored items that do not have a PendingReview inspection.
        // Or simply follow the exact logic:
        var pendingInspection = await db.Set<WarehouseItem>()
            .AsNoTracking()
            .Where(w => w.Status == WarehouseItemStatus.Stored)
            .CountAsync(cancellationToken);

        // 2. Pending Review: Inspections with DecisionStatus == PendingReview
        var pendingReview = await db.Set<WarehouseInspection>()
            .AsNoTracking()
            .Where(i => i.DecisionStatus == WarehouseInspectionDecisionStatus.PendingReview)
            .CountAsync(cancellationToken);

        // 3. Completed Today: Inspections with DecisionStatus != PendingReview AND ReviewedAt >= startOfDay
        var completedToday = await db.Set<WarehouseInspection>()
            .AsNoTracking()
            .Where(i => i.DecisionStatus != WarehouseInspectionDecisionStatus.PendingReview && 
                        i.ReviewedAt >= startOfDay)
            .CountAsync(cancellationToken);

        return new InspectionDashboardStatsDto(pendingInspection, pendingReview, completedToday);
    }
}
