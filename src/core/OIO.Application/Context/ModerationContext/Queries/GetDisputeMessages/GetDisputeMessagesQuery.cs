using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.ModerationContext.DTOs;
using OIO.Application.Context.ModerationContext.Mappings;
using OIO.Application.Context.ModerationContext.Services;
using OIO.Domain.Context.ModerationContext.Aggregates.Disputes;
using OIO.Domain.Context.ModerationContext.ValueObjects.Ids;
using OIO.Domain.Context.UserContext.Aggregates.Users;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.ModerationContext.Queries.GetDisputeMessages;

public sealed record GetDisputeMessagesQuery(
    Guid DisputeId,
    DateTime? BeforeCreatedAt = null,
    Guid? BeforeId = null,
    int? PageSize = null) : IQuery<DisputeMessagePageDto>;

internal sealed class GetDisputeMessagesQueryHandler(
    IDbContext dbContext,
    DisputeAccessService accessService)
    : IQueryHandler<GetDisputeMessagesQuery, DisputeMessagePageDto>
{
    public async Task<Result<DisputeMessagePageDto, Error>> Handle(
        GetDisputeMessagesQuery request,
        CancellationToken cancellationToken)
    {
        var disputeResult = await accessService.GetAccessibleDisputeAsync(
            DisputeId.From(request.DisputeId),
            cancellationToken: cancellationToken);

        if (disputeResult.IsFailure)
            return disputeResult.Error;

        var pageSize = Math.Clamp(request.PageSize ?? 50, 1, 100);
        var query = dbContext.Set<DisputeMessage>()
            .AsNoTracking()
            .Include(x => x.Attachments)
            .Where(x => x.DisputeId == disputeResult.Value.Id);

        if (!accessService.CanViewInternalMessages)
            query = query.Where(x => !x.IsInternal);

        var pageItemsDesc = await query
            .OrderByDescending(x => x.CreatedAt)
            .ThenByDescending(x => x.Id)
            .ToListAsync(cancellationToken);

        if (request.BeforeCreatedAt.HasValue)
        {
            var beforeCreatedAt = request.BeforeCreatedAt.Value;
            pageItemsDesc = pageItemsDesc
                .Where(x => x.CreatedAt < beforeCreatedAt ||
                            (x.CreatedAt == beforeCreatedAt &&
                             (!request.BeforeId.HasValue || x.Id.Value.CompareTo(request.BeforeId.Value) < 0)))
                .ToList();
        }

        pageItemsDesc = pageItemsDesc
            .Take(pageSize + 1)
            .ToList();

        var hasMore = pageItemsDesc.Count > pageSize;
        if (hasMore)
            pageItemsDesc.RemoveAt(pageItemsDesc.Count - 1);

        var senderIds = pageItemsDesc
            .Select(x => x.SenderId)
            .Distinct()
            .ToList();

        var displayNames = await dbContext.Set<User>()
            .AsNoTracking()
            .Where(x => senderIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id.Value, x => x.UserName.Value, cancellationToken);

        var nextCursorItem = pageItemsDesc.LastOrDefault();
        var messages = pageItemsDesc
            .OrderBy(x => x.CreatedAt)
            .ThenBy(x => x.Id.Value)
            .Select(x => x.ToDto(displayNames))
            .ToList();

        return new DisputeMessagePageDto(
            messages,
            hasMore,
            nextCursorItem?.CreatedAt,
            nextCursorItem?.Id.Value);
    }
}
