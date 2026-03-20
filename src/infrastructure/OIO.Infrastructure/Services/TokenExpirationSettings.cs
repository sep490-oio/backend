using Microsoft.Extensions.Options;
using OIO.Application.Context.UserContext.Services;
using OIO.Infrastructure.Settings;

namespace OIO.Infrastructure.Services;

internal sealed class TokenExpirationSettings : ITokenExpirationSettings
{
    private readonly IOptionsMonitor<JwtOptions> _settings;

    public TokenExpirationSettings(IOptionsMonitor<JwtOptions> settings)
    {
        _settings = settings;
    }
    public TimeSpan AccessTokenExpiration => _settings.CurrentValue.AccessTokenExpiration;
    public TimeSpan RefreshTokenExpiration => _settings.CurrentValue.RefreshTokenExpiration;
    public TimeSpan FamilySlidingExpiration => _settings.CurrentValue.RefreshTokenFamilySlidingExpiration;
    public TimeSpan FamilyAbsoluteExpiration => _settings.CurrentValue.RefreshTokenFamilyAbsoluteExpiration;
}