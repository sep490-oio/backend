using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using OIO.Application.Context.UserContext.Services;
using OIO.Domain.Context.UserContext.ValueObjects;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Infrastructure.Authorizations;
using OIO.Infrastructure.Settings;
using JwtRegisteredClaimNames = Microsoft.IdentityModel.JsonWebTokens.JwtRegisteredClaimNames;

namespace OIO.Infrastructure.Services;

internal sealed class JwtTokenProvider : IJwtTokenProvider
{
    private readonly JwtOptions _jwtOptions;
    private const int TokenSizeInBytes = 64;

    public JwtTokenProvider(
        IOptions<JwtOptions> jwtOptions)
    {
        _jwtOptions = jwtOptions.Value;
        
    }

    public string GenerateJwt(
        UserId userId,
        UserEmail email,
        UserName userName, 
        Guid deviceId,
        IReadOnlyCollection<string> roles,
        DateTime nowUtc)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(JwtRegisteredClaimNames.Email, email),
            new(JwtRegisteredClaimNames.Name, userName),
            new(JwtRegisteredClaimNames.Jti, $"{Guid.CreateVersion7()}"),
            new(CustomClaimType.DeviceId, $"{deviceId}"),
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