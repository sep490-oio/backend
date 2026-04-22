namespace OIO.Application.Context.UserContext.DTOs;

public sealed record TermsDocumentDto(
    Guid Id,
    string Type,
    int Version,
    bool IsActive,
    string Status,
    DateTime? PublishedAt,
    DateTime CreatedAt,
    Guid? CreatedBy,
    Guid? ActivatedBy,
    DateTime? ArchivedAt,
    Guid? ArchivedBy,
    string? ArchivedReason,
    string ContentUrl,
    string? FileName,
    long? FileSize,
    string? Format,
    int? Width,
    int? Height,
    double? DurationSeconds,
    string StoragePublicId,
    string StorageFolder);
