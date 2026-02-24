namespace OIO.Infrastructure.Settings;

public class JwtOptions
{
    public const string SectionName = "Jwt";
    
    public string SecretKey { get; init; } = null!;
    public string Issuer { get; init; } = null!;
    public string Audience { get; init; } = null!;
    public TimeSpan AccessTokenExpiration { get; init; } = TimeSpan.FromMinutes(15);
    public TimeSpan RefreshTokenExpiration { get; init; } = TimeSpan.FromDays(7);
    public TimeSpan RefreshTokenFamilySlidingExpiration { get; init; } = TimeSpan.FromDays(7);
    public TimeSpan RefreshTokenFamilyAbsoluteExpiration { get; init; } = TimeSpan.FromDays(180); 
}