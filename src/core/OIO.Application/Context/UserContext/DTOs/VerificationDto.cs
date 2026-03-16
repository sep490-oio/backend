namespace OIO.Application.Context.UserContext.DTOs;

public sealed record VerificationDto(
    Guid Id,
    Guid UserId,
    string VerificationType,
    bool AutoVerified,
    string? FullName,
    DateOnly? DateOfBirth,
    string? Gender,
    string? Nationality,
    VerificationDocumentInfoDto? Document,
    VerificationAddressDto? PermanentAddress,
    string Status,
    DateTime? VerifiedAt,
    Guid? VerifiedBy,
    string? RejectionReason,
    string? RejectionCode,
    DateTime? SubmittedAt,
    DateTime? ExpiresAt,
    int AttemptCount,
    DateTime CreatedAt,
    DateTime? ModifiedAt,
    IReadOnlyCollection<VerificationDocumentDto> Documents);

public sealed record VerificationDocumentInfoDto(
    string IdType,
    string IdNumber,
    DateOnly? IssuedDate,
    DateOnly? ExpiredDate,
    string? IssuedPlace);

public sealed record VerificationAddressDto(
    string FullAddress,
    string Province,
    string District,
    string Ward);

public sealed record VerificationDocumentDto(
    Guid Id,
    string DocumentType,
    string ResourceType,
    string SecureUrl,
    string? FileHash,
    string? MimeType,
    string VerificationStatus,
    DateTime UploadedAt,
    DateTime CreatedAt);

public sealed record VerificationSummaryDto(
    Guid Id,
    string VerificationType,
    bool AutoVerified,
    string? FullName,
    string Status,
    DateTime? SubmittedAt,
    int AttemptCount,
    DateTime CreatedAt);
