namespace OIO.Infrastructure.Settings;

public class CorsOptions
{
    public const string PolicyName = "OIOCorsPolicy";
    public const string SectionName = "Cors";

    public required string[] AllowedOrigins { get; init; }
}