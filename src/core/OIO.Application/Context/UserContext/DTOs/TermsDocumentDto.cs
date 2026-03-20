namespace OIO.Application.Context.UserContext.DTOs;

public sealed record TermsDocumentDto(
    Guid Id,
    string Type,
    int Version,
    bool IsActive,
    DateTime? PublishedAt,
    DateTime CreatedAt,
    string ContentUrl,
    string? FileName,
    long? FileSize,
    string? Format,
    int? Width,
    int? Height,
    double? DurationSeconds,
    string StoragePublicId,
    string StorageFolder);