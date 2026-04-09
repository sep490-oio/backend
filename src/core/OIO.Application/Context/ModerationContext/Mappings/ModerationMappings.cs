using OIO.Application.Context.ModerationContext.DTOs;
using OIO.Domain.Context.ModerationContext.Aggregates;
using OIO.Domain.Context.UserContext.Aggregates.Users;
using OIO.Domain.Context.ModerationContext.Aggregates.Disputes;
using OIO.Domain.Context.OrderContext.ValueObjects.Ids;
using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;
using OIO.Domain.Context.Shared.ValueObjects;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;

namespace OIO.Application.Context.ModerationContext.Mappings;

internal static class ModerationMappings
{
    public static ReportDto ToDto(this Report report)
    {
        return new ReportDto(
            Id: report.Id.Value,
            ReporterId: report.ReporterId.Value,
            EntityType: report.EntityType,
            EntityId: report.EntityId,
            ReasonCode: report.ReasonCode,
            Description: report.Description,
            Attachments: report.Attachments,
            Status: report.Status.Id,
            AssignedTo: report.AssignedTo?.Value,
            CreatedAt: report.CreatedAt,
            AssignedAt: report.AssignedAt,
            ResolvedAt: report.ResolvedAt,
            EscalatedEmergencyAt: report.EscalatedEmergencyAt,
            ResolutionNotes: report.ResolutionNotes,
            DisputeId: report.DisputeId?.Value);
    }

    public static MonitoringAlertDto ToDto(this MonitoringAlert alert)
    {
        return new MonitoringAlertDto(
            Id: alert.Id.Value,
            EntityType: alert.EntityType,
            EntityId: alert.EntityId,
            AlertType: alert.AlertType,
            Severity: alert.Severity.Id,
            Payload: alert.Payload,
            Status: alert.Status.Id,
            Notes: alert.Notes,
            AcknowledgedBy: alert.AcknowledgedBy,
            AcknowledgedAt: alert.AcknowledgedAt,
            ResolvedBy: alert.ResolvedBy,
            ResolvedAt: alert.ResolvedAt,
            CreatedAt: alert.CreatedAt);
    }

    public static UserRiskFlagDto ToDto(this UserRiskFlag flag)
    {
        return new UserRiskFlagDto(
            Id: flag.Id.Value,
            UserId: flag.UserId.Value,
            FlagType: flag.FlagType,
            Reason: flag.Reason,
            Severity: flag.Severity.Id,
            CreatedBy: flag.CreatedBy?.Value,
            CreatedAt: flag.CreatedAt);
    }

    public static DisputeThreadMetaDto ToMetaDto(this Dispute dispute)
    {
        return new DisputeThreadMetaDto(
            dispute.Id.Value,
            dispute.DisputeNumber.Value,
            dispute.Title,
            dispute.Status.Id,
            dispute.Priority.Id,
            dispute.CaseDomain,
            dispute.CaseType,
            dispute.Description,
            ToNullableGuid(dispute.AuctionId),
            ToNullableGuid(dispute.VerificationId),
            ToNullableGuid(dispute.OrderId),
            dispute.ComplainantId.Value,
            dispute.RespondentId.Value,
            dispute.AssignedTo?.Value,
            dispute.CreatedAt,
            dispute.ResolvedAt,
            dispute.ModifiedAt);
    }

    public static DisputeSummaryDto ToSummaryDto(
        this Dispute dispute,
        string? lastMessagePreview,
        DateTime? lastMessageAt,
        int unreadCount)
    {
        return new DisputeSummaryDto(
            dispute.Id.Value,
            dispute.DisputeNumber.Value,
            dispute.Title,
            dispute.Status.Id,
            dispute.Priority.Id,
            ToNullableGuid(dispute.AuctionId),
            ToNullableGuid(dispute.VerificationId),
            ToNullableGuid(dispute.OrderId),
            lastMessagePreview,
            lastMessageAt,
            unreadCount,
            dispute.AssignedTo?.Value,
            dispute.CreatedAt);
    }

    public static DisputeParticipantReadStateDto ToDto(this DisputeParticipantState state)
    {
        return new DisputeParticipantReadStateDto(
            state.DisputeId.Value,
            state.UserId.Value,
            state.LastReadMessageId?.Value,
            state.LastReadAt);
    }

    public static DisputeParticipantDto ToParticipantDto(
        this User user,
        string role,
        DateTime? lastReadAt)
    {
        return new DisputeParticipantDto(
            user.Id.Value,
            user.UserName.Value,
            role,
            lastReadAt,
            user.Profile?.AvatarUrl?.Value);
    }

    public static DisputeMessageDto ToDto(
        this DisputeMessage message,
        IReadOnlyDictionary<Guid, string> displayNames,
        IReadOnlyDictionary<Guid, string?> avatarUrls)
    {
        var senderDisplayName = displayNames.TryGetValue(message.SenderId.Value, out var displayName)
            ? displayName
            : message.SenderId.Value.ToString();

        var senderAvatarUrl = avatarUrls.TryGetValue(message.SenderId.Value, out var av) ? av : null;

        return new DisputeMessageDto(
            message.Id.Value,
            message.DisputeId.Value,
            message.SenderId.Value,
            senderDisplayName,
            senderAvatarUrl,
            message.Message,
            message.IsInternal,
            message.CreatedAt,
            message.Attachments
                .OrderBy(x => x.SortOrder)
                .Select(ToDto)
                .ToList());
    }

    public static DisputeMessageAttachmentDto ToDto(this DisputeMessageAttachment attachment)
    {
        return new DisputeMessageAttachmentDto(
            attachment.Id.Value,
            attachment.Info.FileName,
            ResolveResourceType(attachment.Info),
            attachment.Info.SecureUrl ?? string.Empty,
            attachment.Info.Bytes ?? 0,
            attachment.Info.Format ?? string.Empty,
            attachment.Info.Width,
            attachment.Info.Height,
            attachment.Info.DurationSeconds);
    }

    private static string ResolveResourceType(MediaInfo info)
    {
        if (info.IsVideo) return "video";
        if (info.IsImage) return "image";
        return "raw";
    }

    public static bool IsVisibleTo(this DisputeMessage message, bool canViewInternal)
        => !message.IsInternal || canViewInternal;

    public static int CountUnread(
        this IEnumerable<DisputeMessage> messages,
        DisputeParticipantState? state,
        UserId currentUserId,
        bool canViewInternal)
    {
        var visibleMessages = messages
            .Where(x => x.IsVisibleTo(canViewInternal))
            .Where(x => x.SenderId != currentUserId);

        if (!state?.LastReadAt.HasValue ?? true)
            return visibleMessages.Count();

        var lastReadAt = state?.LastReadAt ?? DateTime.MinValue;
        return visibleMessages.Count(x => x.CreatedAt > lastReadAt);
    }

    public static string? ToPreview(this DisputeMessage? message)
    {
        if (message is null)
            return null;

        if (!string.IsNullOrWhiteSpace(message.Message))
            return message.Message.Length <= 120
                ? message.Message
                : $"{message.Message[..117]}...";

        return message.Attachments.Count > 0 ? "Attachment" : null;
    }

    private static Guid? ToNullableGuid(OrderId orderId)
        => orderId == OrderId.From(Guid.Empty) ? null : orderId.Value;

    private static Guid? ToNullableGuid(AuctionId? auctionId)
        => auctionId?.Value;

    private static Guid? ToNullableGuid(IdentityVerificationId? verificationId)
        => verificationId?.Value;
}
