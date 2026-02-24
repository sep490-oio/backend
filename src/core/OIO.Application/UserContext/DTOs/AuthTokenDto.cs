namespace OIO.Application.UserContext.DTOs;

public sealed record AuthTokenDto(
    string AccessToken,
    string RefreshToken,
    DateTime AccessTokenExpiresAt,
    DateTime RefreshTokenExpiresAt,
    SessionExpirationDto Session);