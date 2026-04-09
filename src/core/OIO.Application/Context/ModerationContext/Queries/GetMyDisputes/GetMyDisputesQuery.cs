using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Commons;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.ModerationContext.DTOs;
using OIO.Application.Context.UserContext.Services;
using OIO.Application.Extensions;
using OIO.Domain.Context.ModerationContext.Aggregates.Disputes;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.ModerationContext.Queries.GetMyDisputes;

public sealed record GetMyDisputesQuery(
    PagedParameters Parameters) : IQuery<PagedList<BuyerDisputeListItemDto>>;

internal sealed class GetMyDisputesQueryHandler(
    IDbContext dbContext,
    ICurrentUser currentUser)
    : IQueryHandler<GetMyDisputesQuery, PagedList<BuyerDisputeListItemDto>>
{
    public async Task<Result<PagedList<BuyerDisputeListItemDto>, Error>> Handle(
        GetMyDisputesQuery request,
        CancellationToken cancellationToken)
    {
        var parameters = request.Parameters ?? new PagedParameters();
        var userId = currentUser.UserId;

        var query = dbContext.Set<Dispute>()
            .AsNoTracking()
            .Where(x => x.ComplainantId == userId || x.RespondentId == userId);

        var count = await query.CountAsync(cancellationToken);

        var disputes = await query
            .OrderByDescending(x => x.ModifiedAt ?? x.CreatedAt)
            .ThenByDescending(x => x.CreatedAt)
            .Page(parameters)
            .ToListAsync(cancellationToken);

        if (disputes.Count == 0)
            return PagedList<BuyerDisputeListItemDto>.ToPagedList(
                Array.Empty<BuyerDisputeListItemDto>(), count, parameters);

        var items = disputes
            .Select(d => new BuyerDisputeListItemDto(
                d.Id.Value,
                d.DisputeNumber.Value,
                d.Status.Id,
                d.CaseDomain,
                d.CaseType,
                d.Title,
                d.CreatedAt,
                d.ModifiedAt))
            .ToList();

        return items.ToPagedList(count, parameters);
    }
}
