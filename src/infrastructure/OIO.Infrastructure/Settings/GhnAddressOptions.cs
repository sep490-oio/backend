namespace OIO.Infrastructure.Settings;

public sealed class GhnAddressOptions
{
    public const string SectionName = "GhnAddress";

    public string Token { get; init; } = string.Empty;
    public string BaseUrl { get; init; } = "https://dev-online-gateway.ghn.vn";
}
