using OIO.Application.Abstractions.Commons;

namespace OIO.Application.Context.ModerationContext.DTOs;

public sealed record AdminDisputeFilterParameters : PagedParameters
{
    public string? Status { get; init; }
    public string? Domain { get; init; }
    public string? CaseType { get; init; }
    public Guid? AssignedToUserId { get; init; }
    public string? Search { get; init; }
}

public sealed record AdminDisputeListItemDto(
    Guid Id,
    string DisputeNumber,
    string Status,
    string? Domain,
    string? CaseType,
    string? PrimaryTargetType,
    string Title,
    string? ComplainantDisplayName,
    string? RespondentDisplayName,
    string? AssignedToDisplayName,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

public sealed record AdminDisputeDetailDto(
    Guid Id,
    string DisputeNumber,
    string Status,
    string? Domain,
    string? CaseType,
    string? PrimaryTargetType,
    string Title,
    string Description,
    string? ContextSnapshotJson,
    string? ComplainantDisplayName,
    string? RespondentDisplayName,
    string? AssignedToDisplayName,
    string? ResolutionOutcome,
    string? ResolutionReason,
    string? ResolutionActionSetJson,
    string? ResolvedByDisplayName,
    DateTime? ResolvedAt,
    Guid? OrderId,
    Guid? AuctionId,
    Guid? ShipmentId,
    Guid? WarehouseItemId,
    Guid? PaymentId,
    Guid? AssignedToUserId,
    DateTime? AssignedAt,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    bool CanAssign,
    bool CanRequestEvidence,
    bool CanAddFinding,
    bool CanResolve,
    bool CanReject,
    DateTime? RequestedEvidenceAt,
    string? RequestedEvidenceByDisplayName,
    IReadOnlyList<AdminDisputeMessageDto> Messages,
    IReadOnlyList<AdminDisputeEvidenceDto> Evidence,
    IReadOnlyList<AdminDisputeFindingDto> Findings);

public sealed record DisputeAssignableUserDto(
    Guid UserId,
    string DisplayName,
    string Role,
    List<string> DomainCapabilities);

public sealed record AdminDisputeMessageAttachmentDto(
    Guid Id,
    string SecureUrl,
    string? FileName,
    string ResourceType,
    string? Format,
    long? Bytes,
    int? Width,
    int? Height,
    decimal? DurationSeconds);

public sealed record AdminDisputeMessageDto(
    Guid Id,
    string AuthorDisplayName,
    string Content,
    string Visibility,
    DateTime CreatedAt,
    IReadOnlyList<AdminDisputeMessageAttachmentDto> Attachments);

public sealed record AdminDisputeEvidenceDto(
    Guid Id,
    Guid SubmittedBy,
    string Type,
    string? Description,
    string? SecureUrl,
    string? FileName,
    string? ResourceType,
    string? Format,
    long? Bytes,
    int? Width,
    int? Height,
    decimal? DurationSeconds,
    DateTime CreatedAt);

public sealed record AdminDisputeFindingDto(
    Guid Id,
    string Domain,
    string AuthorDisplayName,
    string? VerdictRecommendation,
    string Summary,
    string? FindingNote,
    DateTime CreatedAt,
    IReadOnlyList<AdminDisputeFindingReferenceDto> References);

public sealed record AdminDisputeFindingReferenceDto(
    string ReferenceType,
    Guid TargetId,
    string Label,
    string? SecureUrl,
    string? ResourceType,
    string? MessagePreview,
    DateTime? CreatedAt);

public sealed record BuyerDisputeListItemDto(
    Guid Id,
    string DisputeNumber,
    string Status,
    string? Domain,
    string? CaseType,
    string Title,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

public sealed record BuyerDisputeDetailDto(
    Guid Id,
    string DisputeNumber,
    string Status,
    string? Domain,
    string? CaseType,
    string Title,
    string Description,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    IReadOnlyList<AdminDisputeMessageDto> Messages);
