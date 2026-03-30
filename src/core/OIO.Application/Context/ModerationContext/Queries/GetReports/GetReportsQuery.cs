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

namespace OIO.Application.Context.ModerationContext.Queries.GetReports;

public sealed record GetReportsQuery(GetReportsQueryFilters Parameters) 
    : IQuery<PagedList<ReportDto>>;

internal sealed class GetReportsQueryHandler(IDbContext dbContext)
    : IQueryHandler<GetReportsQuery, PagedList<ReportDto>>
{
    public async Task<Result<PagedList<ReportDto>, Error>> Handle(
        GetReportsQuery request,
        CancellationToken cancellationToken)
    {
        var parameters = request.Parameters;

        var query = dbContext.Set<Report>()
            .AsNoTracking()
            .AsQueryable();

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

        query = query.OrderByDescending(x => x.CreatedAt);

        var totalCount = await query.CountAsync(cancellationToken);

        var reports = await query
            .Select(x => x.ToDto())
            .ToPagedListAsync(totalCount, parameters, cancellationToken);

        return reports;
    }
}