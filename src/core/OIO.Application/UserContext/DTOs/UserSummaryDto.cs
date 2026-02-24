namespace OIO.Application.UserContext.DTOs;

public sealed record UserSummaryDto(
    Guid Id,
    string UserName,
    string Email,
    string? DisplayName,
    string? AvatarUrl,
    string Status,
    DateTime CreatedAt);