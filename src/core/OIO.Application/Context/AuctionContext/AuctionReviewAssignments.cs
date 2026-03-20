using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Data;
using OIO.Domain.AppDefinitions;
using OIO.Domain.Context.CatalogContext.Aggregates.Items;
using OIO.Domain.Context.CatalogContext.Enums;
using OIO.Domain.Context.UserContext.Aggregates.Users;
using OIO.Domain.Context.UserContext.Enums;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;

namespace OIO.Application.Context.AuctionContext;

internal static class AuctionReviewAssignments
{
    public static async Task<UserId?> ResolveReviewerIdAsync(
        IDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var reviewerIds = await dbContext.Set<User>()
            .AsNoTracking()
            .Where(user =>
                user.Status == UserStatus.Active &&
                user.Roles.Any(role => role.RoleName == App.Roles.Catalogs.Admin))
            .Select(user => user.Id)
            .ToListAsync(cancellationToken);

        if (reviewerIds.Count == 0)
            return null;

        var workloads = await dbContext.Set<Item>()
            .AsNoTracking()
            .Where(item =>
                item.AssignedAdminId != null &&
                (item.Status == ItemStatus.Draft ||
                 item.Status == ItemStatus.PendingReview ||
                 item.Status == ItemStatus.Rejected))
            .GroupBy(item => item.AssignedAdminId)
            .Select(group => new
            {
                ReviewerId = group.Key,
                AssignedCount = group.Count()
            })
            .ToListAsync(cancellationToken);

        var workloadByReviewer = workloads
            .Where(x => x.ReviewerId.HasValue)
            .ToDictionary(x => x.ReviewerId!.Value, x => x.AssignedCount);

        return reviewerIds
            .OrderBy(id => workloadByReviewer.GetValueOrDefault(id, 0))
            .ThenBy(id => id.Value)
            .FirstOrDefault();
    }
}
