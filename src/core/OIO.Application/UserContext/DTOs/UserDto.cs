namespace OIO.Application.UserContext.DTOs;

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
 