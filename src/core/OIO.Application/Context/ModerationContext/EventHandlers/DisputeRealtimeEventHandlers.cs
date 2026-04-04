using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OIO.Application.Abstractions.Data;
using OIO.Application.Context.ModerationContext.DTOs;
using OIO.Application.Context.ModerationContext.Events;
using OIO.Application.Context.ModerationContext.Mappings;
using OIO.Application.Context.ModerationContext.Services;
using OIO.Application.Context.NotificationContext;
using OIO.Application.Context.NotificationContext.Commands.CreateNotification;
using OIO.Domain.AppDefinitions;
using OIO.Domain.Context.ModerationContext.Aggregates.Disputes;
using OIO.Domain.Context.UserContext.Aggregates.Users;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;

namespace OIO.Application.Context.ModerationContext.EventHandlers;

internal sealed class DisputeMessageSentEventHandler(
    IDbContext dbContext,
    IDisputeRealtimeService realtimeService,
    ISender sender,
    ILogger<DisputeMessageSentEventHandler> logger)
    : INotificationHandler<DisputeMessageSentEvent>
{
    public async Task Handle(DisputeMessageSentEvent notification, CancellationToken cancellationToken)
    {
        var message = await dbContext.Set<DisputeMessage>()
            .AsNoTracking()
            .Include(x => x.Attachments)
            .FirstOrDefaultAsync(
                x => x.Id == notification.MessageId && x.DisputeId == notification.DisputeId,
                cancellationToken);

        var dispute = await dbContext.Set<Dispute>()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == notification.DisputeId, cancellationToken);

        if (message is null || dispute is null)
            return;

        var participantStates = await dbContext.Set<DisputeParticipantState>()
            .AsNoTracking()
            .Where(x => x.DisputeId == notification.DisputeId)
            .ToListAsync(cancellationToken);

        var candidateUserIds = GetCandidateUserIds(dispute, participantStates);
        candidateUserIds.Add(notification.SenderId.Value);

        var candidateIds = candidateUserIds.Select(UserId.From).ToList();
        var users = await dbContext.Set<User>()
            .AsNoTracking()
            .Include(x => x.Roles)
            .Include(x => x.Profile)
            .Where(x => candidateIds.Contains(x.Id))
            .ToListAsync(cancellationToken);

        var displayNames = users.ToDictionary(x => x.Id.Value, x => x.UserName.Value);
        var avatarUrls = users.ToDictionary(x => x.Id.Value, x => x.Profile?.AvatarUrl?.Value);
        var dto = message.ToDto(displayNames, avatarUrls);
        await realtimeService.BroadcastMessageAsync(dispute.Id.Value, dto, notification.IsInternal, cancellationToken);

        var allMessages = await dbContext.Set<DisputeMessage>()
            .AsNoTracking()
            .Where(x => x.DisputeId == notification.DisputeId)
            .ToListAsync(cancellationToken);

        var adminIds = users
            .Where(x => x.Roles.Any(r => r.RoleName == App.Roles.Catalogs.Admin))
            .Select(x => x.Id.Value)
            .ToHashSet();

        var recipientIds = notification.IsInternal
            ? adminIds
            : GetExternalRecipientIds(dispute, adminIds);

        recipientIds.Remove(notification.SenderId.Value);

        foreach (var recipientId in recipientIds)
        {
            var state = participantStates.FirstOrDefault(x => x.UserId.Value == recipientId);
            var unreadCount = allMessages.CountUnread(
                state,
                UserId.From(recipientId),
                adminIds.Contains(recipientId));

            await realtimeService.BroadcastUnreadUpdatedAsync(
                recipientId,
                new DisputeUnreadUpdateDto(dispute.Id.Value, unreadCount),
                cancellationToken);

            // Only send push/email notification if the user has unread messages.
            // Users actively viewing the chat (unreadCount == 0) already see messages via SignalR.
            if (unreadCount > 0)
            {
                await NotificationDispatch.DispatchAsync(
                    sender,
                    logger,
                    new CreateNotificationCommand(
                        recipientId,
                        NotificationType: "moderation",
                        EventType: notification.IsInternal ? "dispute_internal_message_received" : "dispute_message_received",
                        Title: notification.IsInternal ? "New internal dispute message" : "New dispute message",
                        Message: BuildNotificationMessage(dto),
                        EntityType: "dispute",
                        EntityId: dispute.Id.Value,
                        Metadata: NotificationDispatch.SerializeMetadata(new
                        {
                            disputeId = dispute.Id.Value,
                            messageId = dto.Id,
                            isInternal = notification.IsInternal
                        })),
                    cancellationToken);
            }
        }
    }

    private static HashSet<Guid> GetCandidateUserIds(Dispute dispute, IEnumerable<DisputeParticipantState> states)
    {
        var userIds = new HashSet<Guid> { dispute.ComplainantId.Value, dispute.RespondentId.Value };

        if (dispute.AssignedTo is not null)
            userIds.Add(dispute.AssignedTo.Value.Value);

        foreach (var state in states)
            userIds.Add(state.UserId.Value);

        return userIds;
    }

    private static HashSet<Guid> GetExternalRecipientIds(Dispute dispute, IReadOnlySet<Guid> adminIds)
    {
        var recipients = new HashSet<Guid>
        {
            dispute.ComplainantId.Value,
            dispute.RespondentId.Value
        };

        foreach (var adminId in adminIds)
            recipients.Add(adminId);

        return recipients;
    }

    private static string BuildNotificationMessage(DisputeMessageDto message)
    {
        if (!string.IsNullOrWhiteSpace(message.Message))
            return message.Message.Length <= 120
                ? message.Message
                : $"{message.Message[..117]}...";

        return message.Attachments.Count == 1
            ? $"{message.SenderDisplayName} sent an attachment."
            : $"{message.SenderDisplayName} sent {message.Attachments.Count} attachments.";
    }
}

internal sealed class DisputeReadStateUpdatedEventHandler(
    IDbContext dbContext,
    IDisputeRealtimeService realtimeService)
    : INotificationHandler<DisputeReadStateUpdatedEvent>
{
    public async Task Handle(DisputeReadStateUpdatedEvent notification, CancellationToken cancellationToken)
    {
        var state = await dbContext.Set<DisputeParticipantState>()
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.DisputeId == notification.DisputeId && x.UserId == notification.UserId,
                cancellationToken);

        if (state is null)
            return;

        var isInternalMessage = await dbContext.Set<DisputeMessage>()
            .AsNoTracking()
            .Where(x => x.Id == notification.LastReadMessageId)
            .Select(x => x.IsInternal)
            .FirstOrDefaultAsync(cancellationToken);

        var isAdmin = await dbContext.Set<User>()
            .AsNoTracking()
            .Where(x => x.Id == notification.UserId)
            .SelectMany(x => x.Roles)
            .AnyAsync(x => x.RoleName == App.Roles.Catalogs.Admin, cancellationToken);

        var allMessages = await dbContext.Set<DisputeMessage>()
            .AsNoTracking()
            .Where(x => x.DisputeId == notification.DisputeId)
            .ToListAsync(cancellationToken);

        await realtimeService.BroadcastReadStateAsync(
            notification.DisputeId.Value,
            state.ToDto(),
            isInternalMessage,
            cancellationToken);

        var unreadCount = allMessages.CountUnread(state, notification.UserId, isAdmin);
        await realtimeService.BroadcastUnreadUpdatedAsync(
            notification.UserId.Value,
            new DisputeUnreadUpdateDto(notification.DisputeId.Value, unreadCount),
            cancellationToken);
    }
}

internal sealed class DisputeChangedEventHandler(
    IDbContext dbContext,
    IDisputeRealtimeService realtimeService,
    ISender sender,
    ILogger<DisputeChangedEventHandler> logger)
    : INotificationHandler<DisputeChangedEvent>
{
    public async Task Handle(DisputeChangedEvent notification, CancellationToken cancellationToken)
    {
        var dispute = await dbContext.Set<Dispute>()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == notification.DisputeId, cancellationToken);

        if (dispute is null)
            return;

        var meta = dispute.ToMetaDto();
        await realtimeService.BroadcastDisputeUpdatedAsync(dispute.Id.Value, meta, false, cancellationToken);

        var recipientIds = new HashSet<Guid> { dispute.ComplainantId.Value, dispute.RespondentId.Value };
        if (dispute.AssignedTo is not null)
            recipientIds.Add(dispute.AssignedTo.Value.Value);

        foreach (var recipientId in recipientIds)
        {
            await NotificationDispatch.DispatchAsync(
                sender,
                logger,
                new CreateNotificationCommand(
                    recipientId,
                    NotificationType: "moderation",
                    EventType: "dispute_updated",
                    Title: dispute.Status.Id == "resolved" ? "Dispute resolved" : "Dispute updated",
                    Message: dispute.Status.Id == "resolved"
                        ? $"Dispute {dispute.DisputeNumber.Value} has been resolved."
                        : $"Dispute {dispute.DisputeNumber.Value} was updated.",
                    EntityType: "dispute",
                    EntityId: dispute.Id.Value,
                    Metadata: NotificationDispatch.SerializeMetadata(new
                    {
                        disputeId = dispute.Id.Value,
                        status = dispute.Status.Id
                    })),
                cancellationToken);
        }
    }
}
