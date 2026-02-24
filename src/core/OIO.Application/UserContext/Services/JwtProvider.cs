using OIO.Application.UserContext.DTOs;
using OIO.Domain.Context.UserContext.ValueObjects;

namespace OIO.Application.UserContext.Services;

public interface ITokenProvider
{
    string GenerateJwt(
        UserId userId,
        UserEmail email,
        UserName userName,
        IReadOnlyCollection<string> roles,
        DateTime now);
    
    string Generate();
}

public interface IPermissionService
{
    Task<HashSet<string>> GetPermissionsAsync(UserId userId, CancellationToken cancellationToken = default);
    Task InvalidatePermissionsCacheAsync(UserId userId, CancellationToken cancellationToken = default);
}

public interface ITokenExpirationSettings
{
    TimeSpan AccessTokenExpiration { get; }
    TimeSpan RefreshTokenExpiration { get; }
    TimeSpan FamilySlidingExpiration { get; }
    TimeSpan FamilyAbsoluteExpiration { get; }
}

public interface IEmailConfirmationService
{
    Task<string> GenerateTokenAsync(UserId userId, CancellationToken ct = default);
    Task<bool> ValidateTokenAsync(UserId userId, string token, CancellationToken ct = default);
}

public interface ICurrentUser
{
    UserId? UserId { get; }
    UserName? UserName { get; }
    UserEmail? Email { get; }
    bool IsAuthenticated { get; }
    bool IsInRole(string roleName);
}