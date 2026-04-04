using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.ModerationContext.DTOs;
using OIO.Application.Context.ModerationContext.Mappings;
using OIO.Application.Context.ModerationContext.Services;
using OIO.Domain.AppDefinitions;
using OIO.Domain.Context.ModerationContext.Aggregates.Disputes;
using OIO.Domain.Context.ModerationContext.ValueObjects.Ids;
using OIO.Domain.Context.UserContext.Aggregates.Users;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.ModerationContext.Queries.GetDisputeThread;

public sealed record GetDisputeThreadQuery(Guid DisputeId) : IQuery<DisputeThreadDto>;

internal sealed class GetDisputeThreadQueryHandler(
    IDbContext dbContext,
    DisputeAccessService accessService)
    : IQueryHandler<GetDisputeThreadQuery, DisputeThreadDto>
{
    public async Task<Result<DisputeThreadDto, Error>> Handle(
        GetDisputeThreadQuery request,
        CancellationToken cancellationToken)
    {
        var disputeResult = await accessService.GetAccessibleDisputeAsync(
            DisputeId.From(request.DisputeId),
            queryBuilder: query => query
                .Include(x => x.Messages)
                    .ThenInclude(x => x.Attachments),
            cancellationToken: cancellationToken);

        if (disputeResult.IsFailure)
            return disputeResult.Error;

        var dispute = disputeResult.Value;
        var participantStates = await dbContext.Set<DisputeParticipantState>()
            .AsNoTracking()
            .Where(x => x.DisputeId == dispute.Id)
            .ToListAsync(cancellationToken);

        var participantIds = participantStates
            .Select(x => x.UserId)
            .Concat([dispute.ComplainantId, dispute.RespondentId])
            .Concat(dispute.AssignedTo is null ? [] : [dispute.AssignedTo.Value])
            .Distinct()
            .ToList();

        var users = await dbContext.Set<User>()
            .AsNoTracking()
            .Include(x => x.Roles)
            .Include(x => x.Profile)
            .Where(x => participantIds.Contains(x.Id))
            .ToListAsync(cancellationToken);

        var statesByUserId = participantStates.ToDictionary(x => x.UserId.Value);
        var displayNames = users.ToDictionary(x => x.Id.Value, x => x.UserName.Value);
        var avatarUrls = users.ToDictionary(x => x.Id.Value, x => x.Profile?.AvatarUrl?.Value);

        var recentMessages = dispute.Messages
            .Where(x => x.IsVisibleTo(accessService.CanViewInternalMessages))
            .OrderByDescending(x => x.CreatedAt)
            .ThenByDescending(x => x.Id.Value)
            .Take(50)
            .OrderBy(x => x.CreatedAt)
            .ThenBy(x => x.Id.Value)
            .Select(x => x.ToDto(displayNames, avatarUrls))
            .ToList();

        var participants = users
            .Select(user =>
            {
                var role = ResolveParticipantRole(dispute, user);
                statesByUserId.TryGetValue(user.Id.Value, out var state);
                return user.ToParticipantDto(role, state?.LastReadAt);
            })
            .OrderBy(x => x.Role)
            .ThenBy(x => x.DisplayName)
            .ToList();

        DisputeParticipantReadStateDto? readState = null;
        if (statesByUserId.TryGetValue(accessService.CurrentUserId.Value, out var currentState))
            readState = currentState.ToDto();

        return new DisputeThreadDto(
            dispute.ToMetaDto(),
            participants,
            readState,
            recentMessages);
    }

    private static string ResolveParticipantRole(Dispute dispute, User user)
    {
        if (user.Id == dispute.ComplainantId)
            return "complainant";

        if (user.Id == dispute.RespondentId)
            return "respondent";

        if (dispute.AssignedTo == user.Id || user.Roles.Any(x => x.RoleName == App.Roles.Catalogs.Admin))
            return "admin";

        return "participant";
    }
}
