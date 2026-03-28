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
    public string? EntityType { get; init; }
    public Guid? EntityId { get; init; }
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
        var query = dbContext.Set<MonitoringAlert>()
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(parameters.Status))
        {
            var status = parameters.Status.Trim().ToLowerInvariant();
            query = query.Where(x => x.Status.Id == status);
        }

        if (!string.IsNullOrWhiteSpace(parameters.EntityType))
        {
            var entityType = parameters.EntityType.Trim().ToLowerInvariant();
            query = query.Where(x => x.EntityType.ToLower() == entityType);
        }

        if (parameters.EntityId.HasValue)
            query = query.Where(x => x.EntityId == parameters.EntityId.Value);

        var totalCount = await query.CountAsync(cancellationToken);
        
        var alerts = await query
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => x.ToDto())
            .ToPagedListAsync(totalCount, parameters, cancellationToken);

        return alerts;
    }
}
