using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.ModerationContext.DTOs;
using OIO.Application.Context.ModerationContext.Mappings;
using OIO.Domain.Context.ModerationContext.Aggregates;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.ModerationContext.Queries.GetReports;

public sealed record GetReportsQuery(
    string? Status = null,
    string? EntityType = null,
    Guid? EntityId = null) : IQuery<IReadOnlyList<ReportDto>>;

internal sealed class GetReportsQueryHandler(IDbContext dbContext)
    : IQueryHandler<GetReportsQuery, IReadOnlyList<ReportDto>>
{
    public async Task<Result<IReadOnlyList<ReportDto>, Error>> Handle(
        GetReportsQuery request,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Set<Report>()
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            var status = request.Status.Trim().ToLowerInvariant();
            query = query.Where(x => x.Status.Id == status);
        }

        if (!string.IsNullOrWhiteSpace(request.EntityType))
        {
            var entityType = request.EntityType.Trim().ToLowerInvariant();
            query = query.Where(x => x.EntityType.ToLower() == entityType);
        }

        if (request.EntityId.HasValue)
            query = query.Where(x => x.EntityId == request.EntityId.Value);

        var reports = await query
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

        return reports.Select(x => x.ToDto()).ToList();
    }
}
