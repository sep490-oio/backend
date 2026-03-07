namespace OIO.Infrastructure.Settings;

public sealed class HashingOptions
{
    public const string SectionName = "Hashing";

    public string HmacKeyBase64 { get; set; } = string.Empty;
}