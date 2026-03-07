namespace OIO.Infrastructure.Settings;

public class CloudinaryOptions
{
    public const string SectionName = "Cloudinary";

    public string CloudName { get; set; } = null!;

    public string ApiKey { get; set; } = null!;

    public string ApiSecret { get; set; } = null!;
    
    public string BaseUrl { get; set; } = null!;
}