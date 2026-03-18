namespace OIO.Infrastructure.Ekyc;

public sealed class VnptEkycOptions
{
    public const string SectionName = "VnptEkyc";

    public string BaseUrl { get; set; } = "https://api.idg.vnpt.vn";
    public string AccessToken { get; set; } = string.Empty;
    public string TokenId { get; set; } = string.Empty;
    public string TokenKey { get; set; } = string.Empty;
    public string MacAddress { get; set; } = "OIO_SERVER";

    public double ApproveThreshold { get; set; } = 80.0;
    public double RejectThreshold { get; set; } = 50.0;
}
