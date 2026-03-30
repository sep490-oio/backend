using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Commons;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.ModerationContext.DTOs;
using OIO.Application.Context.ModerationContext.Mappings;
using OIO.Application.Extensions;
using OIO.Application.Context.UserContext.Services;
using OIO.Domain.Context.ModerationContext.Aggregates;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.ModerationContext.Queries.GetMyReports;

public sealed record GetMyReportsQuery(GetMyReportsQueryFilters Parameters) 
    : IQuery<PagedList<ReportDto>>;

internal sealed class GetMyReportsQueryHandler(
    IDbContext dbContext,
    ICurrentUser currentUser)
    : IQueryHandler<GetMyReportsQuery, PagedList<ReportDto>>
{
    public async Task<Result<PagedList<ReportDto>, Error>> Handle(
        GetMyReportsQuery request,
        CancellationToken cancellationToken)
    {
        var parameters = request.Parameters;

        var query = dbContext.Set<Report>()
            .AsNoTracking()
            .Where(x => x.ReporterId == currentUser.UserId)
            .OrderByDescending(x => x.CreatedAt);

        var totalCount = await query.CountAsync(cancellationToken);

        var reports = await query
            .Select(x => x.ToDto())
            .ToPagedListAsync(totalCount, parameters, cancellationToken);

        return reports;
    }
}