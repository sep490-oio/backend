namespace OIO.Application.Context.UserContext.DTOs;

public sealed record UserDto(
    Guid Id,
    string UserName,
    string Email,
    bool EmailConfirmed,
    string? PhoneNumber,
    string? CountryCode,
    bool PhoneNumberConfirmed,
    bool TwoFactorEnabled,
    string TwoFactorProvider,
    string Status,
    DateTime CreatedAt,
    UserProfileDto? Profile);
 