using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using OIO.Infrastructure.Settings;

namespace OIO.Infrastructure.Authorizations;

public class JwtBearerOptionsSetup : IConfigureNamedOptions<JwtBearerOptions>
{
    private readonly JwtOptions _jwtAuthOptions;
    public JwtBearerOptionsSetup(IOptions<JwtOptions> jwtAuthOptions)
    {
        _jwtAuthOptions = jwtAuthOptions.Value;
    }
    public void Configure(string? name, JwtBearerOptions options)
    {
        if (name != JwtBearerDefaults.AuthenticationScheme) return;
        Configure(options);
    }

    public void Configure(JwtBearerOptions options)
    {
        var secretKey = string.IsNullOrWhiteSpace(_jwtAuthOptions.SecretKey) ?
            throw new InvalidOperationException("Secret key is missing!!!") :
            _jwtAuthOptions.SecretKey;

        options.MapInboundClaims = false; 
        
        options.TokenValidationParameters = new TokenValidationParameters()
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),

            ValidateIssuer = true,
            ValidIssuer = _jwtAuthOptions.Issuer,

            ValidateAudience = true,
            ValidAudience = _jwtAuthOptions.Audience,

            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1),

            NameClaimType = JwtRegisteredClaimNames.Name,
            RoleClaimType = CustomClaimType.Role
        };
    }
}