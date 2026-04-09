using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Commons;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.ModerationContext.DTOs;
using OIO.Application.Extensions;
using OIO.Domain.Context.ModerationContext.Aggregates.Disputes;
using OIO.Domain.Context.UserContext.Aggregates.Users;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.ModerationContext.Queries.GetAdminDisputes;

public sealed record GetAdminDisputesQuery(
    AdminDisputeFilterParameters Parameters) : IQuery<PagedList<AdminDisputeListItemDto>>;

internal sealed class GetAdminDisputesQueryHandler(
    IDbContext dbContext)
    : IQueryHandler<GetAdminDisputesQuery, PagedList<AdminDisputeListItemDto>>
{
    public async Task<Result<PagedList<AdminDisputeListItemDto>, Error>> Handle(
        GetAdminDisputesQuery request,
        CancellationToken cancellationToken)
    {
        var parameters = request.Parameters ?? new AdminDisputeFilterParameters();
        var query = dbContext.Set<Dispute>()
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(parameters.Status))
        {
            var status = parameters.Status.Trim().ToLowerInvariant();
            query = query.Where(x => x.Status.Id == status);
        }

        if (!string.IsNullOrWhiteSpace(parameters.Domain))
            query = query.Where(x => x.CaseDomain == parameters.Domain);

        if (!string.IsNullOrWhiteSpace(parameters.CaseType))
            query = query.Where(x => x.CaseType == parameters.CaseType);

        if (parameters.AssignedToUserId.HasValue)
            query = query.Where(x => x.AssignedToUserId == parameters.AssignedToUserId.Value);

        if (!string.IsNullOrWhiteSpace(parameters.Search))
        {
            var search = parameters.Search.Trim().ToLowerInvariant();
            query = query.Where(x =>
                x.DisputeNumber.Value.ToLower().Contains(search) ||
                x.Title.ToLower().Contains(search));
        }

        var count = await query.CountAsync(cancellationToken);

        var disputes = await query
            .OrderByDescending(x => x.ModifiedAt ?? x.CreatedAt)
            .ThenByDescending(x => x.CreatedAt)
            .Page(parameters)
            .ToListAsync(cancellationToken);

        if (disputes.Count == 0)
            return PagedList<AdminDisputeListItemDto>.ToPagedList(
                Array.Empty<AdminDisputeListItemDto>(), count, parameters);

        // Batch-load display names
        var userIds = disputes
            .SelectMany(d => new[]
            {
                d.ComplainantId.Value,
                d.RespondentId.Value,
                d.AssignedToUserId
            })
            .Where(id => id.HasValue || id is Guid)
            .Select(id => id ?? Guid.Empty)
            .Where(id => id != Guid.Empty)
            .Distinct()
            .Select(UserId.From)
            .ToList();

        var users = await dbContext.Set<User>()
            .AsNoTracking()
            .Where(u => userIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id.Value, u => u.UserName.Value, cancellationToken);

        string? DisplayName(Guid? id) =>
            id.HasValue && users.TryGetValue(id.Value, out var name) ? name : null;

        string? DisplayNameFromUserId(UserId id) =>
            users.TryGetValue(id.Value, out var name) ? name : null;

        var items = disputes
            .Select(d => new AdminDisputeListItemDto(
                d.Id.Value,
                d.DisputeNumber.Value,
                d.Status.Id,
                d.CaseDomain,
                d.CaseType,
                d.PrimaryTargetType,
                d.Title,
                DisplayNameFromUserId(d.ComplainantId),
                DisplayNameFromUserId(d.RespondentId),
                DisplayName(d.AssignedToUserId),
                d.CreatedAt,
                d.ModifiedAt))
            .ToList();

        return items.ToPagedList(count, parameters);
    }
}
