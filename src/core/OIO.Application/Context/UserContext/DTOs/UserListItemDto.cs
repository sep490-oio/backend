namespace OIO.Application.Context.UserContext.DTOs;

public sealed record UserListItemDto(
    Guid Id,
    string UserName,
    string Email,
    string? FirstName,
    string? LastName,
    string Status,
    bool EmailConfirmed,
    IReadOnlyList<string> Roles,
    DateTime CreatedAt);