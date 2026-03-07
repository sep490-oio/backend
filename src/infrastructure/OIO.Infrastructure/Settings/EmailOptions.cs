namespace OIO.Infrastructure.Settings;

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