using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Commons;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.ModerationContext.DTOs;
using OIO.Application.Context.ModerationContext.Mappings;
using OIO.Application.Context.ModerationContext.Services;
using OIO.Application.Extensions;
using OIO.Domain.Context.ModerationContext.Aggregates.Disputes;
using OIO.Domain.Context.ModerationContext.ValueObjects.Ids;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.ModerationContext.Queries.GetAccessibleDisputes;

public sealed record GetAccessibleDisputesQuery(
    DisputeFilterParameters Parameters) : IQuery<PagedList<DisputeSummaryDto>>;

internal sealed class GetAccessibleDisputesQueryHandler(
    IDbContext dbContext,
    DisputeAccessService accessService)
    : IQueryHandler<GetAccessibleDisputesQuery, PagedList<DisputeSummaryDto>>
{
    public async Task<Result<PagedList<DisputeSummaryDto>, Error>> Handle(
        GetAccessibleDisputesQuery request,
        CancellationToken cancellationToken)
    {
        var parameters = request.Parameters ?? new DisputeFilterParameters();
        var query = dbContext.Set<Dispute>()
            .AsNoTracking()
            .AsQueryable();

        if (!accessService.IsAdmin)
        {
            var currentUserId = accessService.CurrentUserId;
            query = query.Where(x => x.ComplainantId == currentUserId || x.RespondentId == currentUserId);
        }

        if (!string.IsNullOrWhiteSpace(parameters.Status))
        {
            var status = parameters.Status.Trim().ToLowerInvariant();
            query = query.Where(x => x.Status.Id == status);
        }

        if (parameters.AssignedTo.HasValue)
        {
            var assignedTo = UserId.From(parameters.AssignedTo.Value);
            query = query.Where(x => x.AssignedTo == assignedTo);
        }

        var count = await query.CountAsync(cancellationToken);

        var disputes = await query
            .OrderByDescending(x => x.ModifiedAt ?? x.CreatedAt)
            .ThenByDescending(x => x.CreatedAt)
            .Page(parameters)
            .ToListAsync(cancellationToken);

        if (disputes.Count == 0)
            return PagedList<DisputeSummaryDto>.ToPagedList(Array.Empty<DisputeSummaryDto>(), count, parameters);

        var disputeIds = disputes.Select(x => x.Id).ToList();
        var canViewInternalMessages = accessService.CanViewInternalMessages;
        var visibleMessagesQuery = dbContext.Set<DisputeMessage>()
            .AsNoTracking()
            .Include(x => x.Attachments)
            .Where(x => disputeIds.Contains(x.DisputeId));

        if (!canViewInternalMessages)
            visibleMessagesQuery = visibleMessagesQuery.Where(x => !x.IsInternal);

        var visibleMessages = await visibleMessagesQuery.ToListAsync(cancellationToken);

        var states = await dbContext.Set<DisputeParticipantState>()
            .AsNoTracking()
            .Where(x => disputeIds.Contains(x.DisputeId) && x.UserId == accessService.CurrentUserId)
            .ToDictionaryAsync(x => x.DisputeId, cancellationToken);

        var messagesByDispute = visibleMessages
            .GroupBy(x => x.DisputeId)
            .ToDictionary(x => x.Key, x => x.OrderBy(y => y.CreatedAt).ThenBy(y => y.Id.Value).ToList());

        var summaries = disputes
            .Select(dispute =>
            {
                messagesByDispute.TryGetValue(dispute.Id, out var messages);
                messages ??= [];
                states.TryGetValue(dispute.Id, out var state);

                var lastMessage = messages
                    .OrderByDescending(x => x.CreatedAt)
                    .ThenByDescending(x => x.Id.Value)
                    .FirstOrDefault();

                var unreadCount = messages.CountUnread(
                    state,
                    accessService.CurrentUserId,
                    canViewInternalMessages);

                return dispute.ToSummaryDto(
                    lastMessage?.ToPreview(),
                    lastMessage?.CreatedAt,
                    unreadCount);
            })
            .ToList();

        return summaries.ToPagedList(count, parameters);
    }
}
