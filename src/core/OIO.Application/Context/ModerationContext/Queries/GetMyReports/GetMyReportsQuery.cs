using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.ModerationContext.DTOs;
using OIO.Application.Context.ModerationContext.Mappings;
using OIO.Application.Context.UserContext.Services;
using OIO.Domain.Context.ModerationContext.Aggregates;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.ModerationContext.Queries.GetMyReports;

public sealed record GetMyReportsQuery() : IQuery<IReadOnlyList<ReportDto>>;

internal sealed class GetMyReportsQueryHandler(
    IDbContext dbContext,
    ICurrentUser currentUser)
    : IQueryHandler<GetMyReportsQuery, IReadOnlyList<ReportDto>>
{
    public async Task<Result<IReadOnlyList<ReportDto>, Error>> Handle(
        GetMyReportsQuery request,
        CancellationToken cancellationToken)
    {
        var reports = await dbContext.Set<Report>()
            .AsNoTracking()
            .Where(x => x.ReporterId == currentUser.UserId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

        return reports.Select(x => x.ToDto()).ToList();
    }
}
