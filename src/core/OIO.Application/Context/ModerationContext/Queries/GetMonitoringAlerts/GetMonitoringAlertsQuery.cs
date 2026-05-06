using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Commons;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.ModerationContext.DTOs;
using OIO.Application.Context.ModerationContext.Mappings;
using OIO.Application.Extensions;
using OIO.Domain.Context.ModerationContext.Aggregates;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.ModerationContext.Queries.GetMonitoringAlerts;

public record GetMonitoringAlertsQueryFilter : PagedParameters
{
    public string? Status { get; init; }
    public string? Severity { get; init; }
    public string? AlertType { get; init; }
    public string? EntityType { get; init; }
    public Guid? EntityId { get; init; }
    public Guid? AssignedTo { get; init; }
    public DateTime? CreatedFrom { get; init; }
    public DateTime? CreatedTo { get; init; }
    public int? MinScore { get; init; }
    public string? Keyword { get; init; }
    public string? SortBy { get; init; }
}

public sealed record GetMonitoringAlertsQuery(GetMonitoringAlertsQueryFilter Parameters) : IQuery<PagedList<MonitoringAlertDto>>;

internal sealed class GetMonitoringAlertsQueryHandler(IDbContext dbContext)
    : IQueryHandler<GetMonitoringAlertsQuery, PagedList<MonitoringAlertDto>>
{
    public async Task<Result<PagedList<MonitoringAlertDto>, Error>> Handle(
        GetMonitoringAlertsQuery request,
        CancellationToken cancellationToken)
    {
        var parameters = request.Parameters;
        var query = ApplyDatabaseFilters(dbContext.Set<MonitoringAlert>().AsNoTracking(), parameters);

        var requiresClientFiltering = parameters.MinScore.HasValue || !string.IsNullOrWhiteSpace(parameters.Keyword);
        if (requiresClientFiltering)
        {
            var allRows = await ApplySort(query, parameters.SortBy)
                .ToListAsync(cancellationToken);

            var filteredDtos = allRows
                .Select(x => x.ToDto())
                .Where(x => MatchesClientFilters(x, parameters))
                .ToList();

            var pagedItems = filteredDtos
                .Skip((parameters.EffectivePageNumber - 1) * parameters.EffectivePageSize)
                .Take(parameters.EffectivePageSize)
                .ToArray();

            return pagedItems.ToPagedList(filteredDtos.Count, parameters);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var alertRows = await ApplySort(query, parameters.SortBy)
            .Page(parameters)
            .ToListAsync(cancellationToken);

        return alertRows
            .Select(x => x.ToDto())
            .ToArray()
            .ToPagedList(totalCount, parameters);
    }

    private static IQueryable<MonitoringAlert> ApplyDatabaseFilters(
        IQueryable<MonitoringAlert> query,
        GetMonitoringAlertsQueryFilter parameters)
    {
        var statusParameter = parameters.Status;
        if (IsSpecified(statusParameter))
        {
            var status = statusParameter!.Trim().ToLowerInvariant();
            query = query.Where(x => x.Status.Id == status);
        }

        var severityParameter = parameters.Severity;
        if (IsSpecified(severityParameter))
        {
            var severity = severityParameter!.Trim().ToLowerInvariant();
            query = query.Where(x => x.Severity.Id == severity);
        }

        var alertTypeParameter = parameters.AlertType;
        if (IsSpecified(alertTypeParameter))
        {
            var alertType = alertTypeParameter!.Trim();
            query = query.Where(x => EF.Functions.ILike(x.AlertType, $"%{alertType}%"));
        }

        var entityTypeParameter = parameters.EntityType;
        if (IsSpecified(entityTypeParameter))
        {
            var entityType = entityTypeParameter!.Trim();
            query = query.Where(x => EF.Functions.ILike(x.EntityType, entityType));
        }

        if (parameters.EntityId.HasValue)
            query = query.Where(x => x.EntityId == parameters.EntityId.Value);

        if (parameters.AssignedTo.HasValue)
            query = query.Where(x => x.AssignedTo == parameters.AssignedTo.Value);

        if (parameters.CreatedFrom.HasValue)
            query = query.Where(x => x.CreatedAt >= parameters.CreatedFrom.Value);

        if (parameters.CreatedTo.HasValue)
            query = query.Where(x => x.CreatedAt <= parameters.CreatedTo.Value);

        return query;
    }

    private static IQueryable<MonitoringAlert> ApplySort(IQueryable<MonitoringAlert> query, string? sortBy)
    {
        return sortBy?.Trim().ToLowerInvariant() switch
        {
            "createdat" or "createdat asc" => query.OrderBy(x => x.CreatedAt),
            "severity" or "severity asc" => query
                .OrderByDescending(x => x.Severity.Id == "critical")
                .ThenByDescending(x => x.Severity.Id == "high")
                .ThenByDescending(x => x.Severity.Id == "medium")
                .ThenByDescending(x => x.CreatedAt),
            "severity desc" => query
                .OrderByDescending(x => x.Severity.Id == "low")
                .ThenByDescending(x => x.Severity.Id == "medium")
                .ThenByDescending(x => x.Severity.Id == "high")
                .ThenByDescending(x => x.CreatedAt),
            _ => query
                .OrderByDescending(x => x.Status.Id == "open")
                .ThenByDescending(x => x.Severity.Id == "critical")
                .ThenByDescending(x => x.Severity.Id == "high")
                .ThenByDescending(x => x.Severity.Id == "medium")
                .ThenByDescending(x => x.CreatedAt)
        };
    }

    private static bool MatchesClientFilters(MonitoringAlertDto alert, GetMonitoringAlertsQueryFilter parameters)
    {
        if (parameters.MinScore.HasValue && (!alert.Score.HasValue || alert.Score.Value < parameters.MinScore.Value))
            return false;

        if (string.IsNullOrWhiteSpace(parameters.Keyword))
            return true;

        var keyword = parameters.Keyword.Trim();
        return Contains(alert.AlertType, keyword) ||
               Contains(alert.AlertTitle, keyword) ||
               Contains(alert.Summary, keyword) ||
               Contains(alert.EntityType, keyword) ||
               Contains(alert.EntityId.ToString(), keyword) ||
               Contains(alert.Id.ToString(), keyword) ||
               alert.Participants.Any(x => Contains(x.UserId.ToString(), keyword) || Contains(x.Role, keyword)) ||
               alert.EvidenceRefs.Any(x => Contains(x.IdOrValue, keyword) || Contains(x.Label, keyword));
    }

    private static bool Contains(string? value, string keyword) =>
        value?.Contains(keyword, StringComparison.OrdinalIgnoreCase) == true;

    private static bool IsSpecified(string? value) =>
        !string.IsNullOrWhiteSpace(value) &&
        !value.Equals("all", StringComparison.OrdinalIgnoreCase);
}
