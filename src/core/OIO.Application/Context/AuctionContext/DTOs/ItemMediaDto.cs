namespace OIO.Application.Context.AuctionContext.DTOs;

public sealed record ItemMediaDto(
    Guid Id,
    string Url,
    string PublicId,
    string ResourceType,
    bool IsPrimary,
    int SortOrder,
    string? FileName,
    long? Bytes,
    string? Format,
    int? Width,
    int? Height,
    double? DurationSeconds);