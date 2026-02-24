using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using OIO.Application.Abstractions.Caching;
using OIO.Application.UserContext.Services;
using OIO.Domain.Context.UserContext.Services;
using OIO.Domain.Context.UserContext.ValueObjects;
using OIO.Infrastructure.Authorizations;
using OIO.Infrastructure.Settings;
using JwtRegisteredClaimNames = Microsoft.IdentityModel.JsonWebTokens.JwtRegisteredClaimNames;

namespace OIO.Infrastructure.Services;

internal sealed class TokenHasher : ITokenHasher
{
    public string Hash(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToBase64String(bytes);
    }

    public bool Verify(string token, string hashedToken)
    {
        var hash = Hash(token);
        return string.Equals(hash, hashedToken, StringComparison.Ordinal);
    }
}

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

internal sealed class TokenProvider : ITokenProvider
{
    private readonly JwtOptions _jwtOptions;
    private const int TokenSizeInBytes = 64;

    public TokenProvider(
        IOptions<JwtOptions> jwtOptions)
    {
        _jwtOptions = jwtOptions.Value;
    }

    public string GenerateJwt(
        UserId userId,
        UserEmail email,
        UserName userName, 
        IReadOnlyCollection<string> roles,
        DateTime nowUtc)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.GetValueAsString()),
            new(JwtRegisteredClaimNames.Email, email),
            new(JwtRegisteredClaimNames.Name, userName),
            new(JwtRegisteredClaimNames.Jti, $"{Guid.CreateVersion7()}"),
            new(JwtRegisteredClaimNames.Iat,
                new DateTimeOffset(nowUtc).ToUnixTimeSeconds().ToString(),
                ClaimValueTypes.Integer64)
        };
        claims.AddRange(roles.Select(role => new Claim(CustomClaimType.Role, role)));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.SecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwtOptions.Issuer,
            audience: _jwtOptions.Audience,
            claims: claims,
            expires: nowUtc.Add(_jwtOptions.AccessTokenExpiration),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
    
    public string Generate()
    {
        var randomBytes = RandomNumberGenerator.GetBytes(TokenSizeInBytes);
        var rawToken = Convert.ToBase64String(randomBytes);
        return rawToken;
    }
}

public sealed class CurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUser(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public UserId? UserId
    {
        get
        {
            var idClaim = _httpContextAccessor.HttpContext?.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
            
            return string.IsNullOrWhiteSpace(idClaim) 
                ? null 
                : UserId.From(Guid.Parse(idClaim)); 
        }
    }

    public UserName? UserName => UserName.Create(_httpContextAccessor.HttpContext?.User.FindFirst(JwtRegisteredClaimNames.Name)?.Value!).GetValueOrDefault();

    public UserEmail? Email => UserEmail.Create(_httpContextAccessor.HttpContext?.User.FindFirst(JwtRegisteredClaimNames.Email)?.Value!).GetValueOrDefault();

    public bool IsAuthenticated => _httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated ?? false;

    public bool IsInRole(string roleName) =>
        _httpContextAccessor.HttpContext?.User.IsInRole(roleName.ToUpperInvariant()) ?? false;

}

internal sealed class EmailConfirmationService : IEmailConfirmationService
{
    private const int TokenExpirationMinutes = 60;
    private const string CachePrefix = "email_confirmation:";

    private readonly HybridCache _cache;

    public EmailConfirmationService(HybridCache cache)
    {
        _cache = cache;
    }

    public async Task<string> GenerateTokenAsync(
        UserId userId, CancellationToken ct = default)
    {
        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        var cacheKey = $"{CachePrefix}{userId.GetValueAsString()}";
        var options = new HybridCacheEntryOptions
        {
            Expiration = TimeSpan.FromMinutes(TokenExpirationMinutes),
            LocalCacheExpiration = TimeSpan.FromMinutes(TokenExpirationMinutes)
        };
        await _cache.SetAsync(
            cacheKey,
            token,
            options,
            cancellationToken: ct);

        return token;
    }

    public async Task<bool> ValidateTokenAsync(
        UserId userId, string token, CancellationToken ct = default)
    {
        var cacheKey = $"{CachePrefix}{userId.GetValueAsString()}";
        var storedToken = await _cache.TryGetValueAsync<string>(cacheKey, ct);

        if (!storedToken.Exists || !string.Equals(storedToken.Value, token, StringComparison.Ordinal))
            return false;

        // Remove token after successful validation (one-time use)
        await _cache.RemoveAsync(cacheKey, ct);
        return true;
    }
}

internal sealed class PhoneVerificationService : IPhoneVerificationService
{
    private const int CodeExpirationMinutes = 5;
    private const int CodeLength = 6;
    private const string CachePrefix = "phone_verification:";

    private readonly HybridCache _cache;
    private readonly ILogger<PhoneVerificationService> _logger;

    public PhoneVerificationService(
        HybridCache cache,
        ILogger<PhoneVerificationService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task SendCodeAsync(
        UserId userId, PhoneNumber phoneNumber, CancellationToken ct = default)
    {
        var code = GenerateCode();
        var cacheKey = $"{CachePrefix}{userId.GetValueAsString()}:{phoneNumber.Value}";

        var options = new HybridCacheEntryOptions
        {
            Expiration = TimeSpan.FromMinutes(CodeExpirationMinutes),
            LocalCacheExpiration = TimeSpan.FromMinutes(CodeExpirationMinutes)
        };
        await _cache.SetAsync(
            cacheKey,
            code,
            options,
            cancellationToken: ct);

        // TODO: Send SMS via external provider (Twilio, etc.)
        _logger.LogInformation(
            "Phone verification code sent to {PhoneNumber} for user {UserId}",
            phoneNumber, userId);
    }

    public async Task<bool> VerifyCodeAsync(
        UserId userId, PhoneNumber phoneNumber, string code, CancellationToken ct = default)
    {
        var cacheKey = $"{CachePrefix}{userId}:{phoneNumber.Value}";
        var storedCode = await _cache.TryGetValueAsync<string>(cacheKey, ct);

        if (!storedCode.Exists || !string.Equals(storedCode.Value, code, StringComparison.Ordinal))
            return false;

        await _cache.RemoveAsync(cacheKey, ct);
        return true;
    }

    private static string GenerateCode()
    {
        return Random.Shared.Next(0, (int)Math.Pow(10, CodeLength))
            .ToString()
            .PadLeft(CodeLength, '0');
    }
}