using Microsoft.Extensions.Options;
using OIO.Application.UserContext.Services;
using OIO.Infrastructure.Settings;

namespace OIO.Infrastructure.Services;

internal sealed class TokenExpirationSettings : ITokenExpirationSettings
{
    private readonly JwtOptions _settings;

    public TokenExpirationSettings(IOptions<JwtOptions> settings)
    {
        _settings = settings.Value;
    }
    public TimeSpan AccessTokenExpiration => _settings.AccessTokenExpiration;
    public TimeSpan RefreshTokenExpiration => _settings.RefreshTokenExpiration;
    public TimeSpan FamilySlidingExpiration => _settings.RefreshTokenFamilySlidingExpiration;
    public TimeSpan FamilyAbsoluteExpiration => _settings.RefreshTokenFamilyAbsoluteExpiration;
}