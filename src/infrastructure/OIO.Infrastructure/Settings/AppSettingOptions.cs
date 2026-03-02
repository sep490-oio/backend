namespace OIO.Infrastructure.Settings;

public sealed class DefaultAccountOptions
{
    public const string SectionName = "DefaultAccount";
    public string Email { get; init; } = null!;
    public string UserName { get; init; } = null!;
    public string Password { get; init; } = null!;
    public string FirstName { get; init; } = null!;
    public string LastName { get; init; } = null!;
    public string DisplayName { get; init; } = null!;

}

public class CorsOptions
{
    public const string PolicyName = "OIOCorsPolicy";
    public const string SectionName = "Cors";

    public required string[] AllowedOrigins { get; init; }
}

public sealed class EmailOptions
{
    public const string SectionName = "Email";
    public string Host { get; init; } = null!;
    public int Port { get; init; } = 587;

    public string? Username { get; init; }
    public string? Password { get; init; }

    public bool UseStartTls { get; init; } = true;

    public string FromAddress { get; init; } = null!;
    public string FromName { get; init; } = "OIO";
}