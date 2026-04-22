namespace OIO.Application.Context.OrderContext.DTOs;

/// <summary>
/// API projection of <c>OrderReturnEvidence</c>. Matches FE shape with nested
/// <see cref="OrderReturnEvidenceMediaDto"/> so photo URLs + media metadata are
/// accessible via <c>evidence.mediaUpload.secureUrl</c>.
/// </summary>
public sealed record OrderReturnEvidenceDto(
    Guid Id,
    Guid OrderReturnId,
    string Category,
    OrderReturnEvidenceMediaDto MediaUpload,
    DateTime CreatedAt,
    Guid CreatedBy);

/// <summary>
/// Media-upload projection nested inside <see cref="OrderReturnEvidenceDto"/>.
/// Mirrors the FE's <c>OrderReturnEvidenceMediaDto</c> type.
/// </summary>
public sealed record OrderReturnEvidenceMediaDto(
    Guid Id,
    string? SecureUrl,
    string? FileName,
    string ResourceType);
