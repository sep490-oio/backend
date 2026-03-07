namespace OIO.Application.Context.AuctionContext.DTOs;

public sealed record CategoryDto(
    Guid Id,
    Guid? ParentId,
    string Name,
    string Slug,
    string? Description,
    string? IconUrl,
    bool IsActive,
    int SortOrder,
    string Path,
    DateTime CreatedAt);