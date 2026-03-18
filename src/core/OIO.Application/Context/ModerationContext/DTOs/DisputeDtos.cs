using OIO.Application.Abstractions.Commons;

namespace OIO.Application.Context.ModerationContext.DTOs;

public sealed record DisputeMessageAttachmentDto(
    Guid Id,
    string? FileName,
    string ResourceType,
    string SecureUrl,
    long Bytes,
    string Format,
    int? Width,
    int? Height,
    double? DurationSeconds);

public sealed record DisputeMessageDto(
    Guid Id,
    Guid DisputeId,
    Guid SenderId,
    string SenderDisplayName,
    string Message,
    bool IsInternal,
    DateTime CreatedAt,
    IReadOnlyList<DisputeMessageAttachmentDto> Attachments);

public sealed record DisputeParticipantReadStateDto(
    Guid DisputeId,
    Guid UserId,
    Guid? LastReadMessageId,
    DateTime? LastReadAt);

public sealed record DisputeParticipantDto(
    Guid UserId,
    string DisplayName,
    string Role,
    DateTime? LastReadAt);

public sealed record DisputeThreadMetaDto(
    Guid Id,
    string DisputeNumber,
    string Title,
    string Status,
    string Priority,
    Guid? AuctionId,
    Guid? VerificationId,
    Guid? OrderId,
    Guid ComplainantId,
    Guid RespondentId,
    Guid? AssignedTo,
    DateTime CreatedAt,
    DateTime? ResolvedAt,
    DateTime? ModifiedAt);

public sealed record DisputeSummaryDto(
    Guid Id,
    string DisputeNumber,
    string Title,
    string Status,
    string Priority,
    Guid? AuctionId,
    Guid? VerificationId,
    Guid? OrderId,
    string? LastMessagePreview,
    DateTime? LastMessageAt,
    int UnreadCount,
    Guid? AssignedTo,
    DateTime CreatedAt);

public sealed record DisputeThreadDto(
    DisputeThreadMetaDto Meta,
    IReadOnlyList<DisputeParticipantDto> Participants,
    DisputeParticipantReadStateDto? CurrentUserReadState,
    IReadOnlyList<DisputeMessageDto> RecentMessages);

public sealed record DisputeUnreadUpdateDto(
    Guid DisputeId,
    int UnreadCount);

public sealed record DisputeMessagePageDto(
    IReadOnlyList<DisputeMessageDto> Messages,
    bool HasMore,
    DateTime? NextBeforeCreatedAt,
    Guid? NextBeforeId);

public sealed record DisputeFilterParameters : PagedParameters
{
    public string? Status { get; init; }
    public Guid? AssignedTo { get; init; }
}
