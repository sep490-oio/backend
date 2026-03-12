using CSharpFunctionalExtensions;

namespace OIO.Domain.Context.Shared.ValueObjects;

public sealed class MediaInfo : ValueObject
{
    public string SecureUrl { get; }
    public string? FileName { get; }
    public long? Bytes { get; }
    public string? Format { get; }
    public int? Width { get; }
    public int? Height { get; }
    public double? DurationSeconds { get; }

    public MediaInfo()
    {
        
    }
    
    private MediaInfo(
        string? secureUrl, 
        string? fileName,
        long? bytes,
        string? format,
        int? width,
        int? height,
        double? durationSeconds)
    {
        SecureUrl = secureUrl;
        FileName = fileName;
        Bytes = bytes;
        Format = format;
        Width = width;
        Height = height;
        DurationSeconds = durationSeconds;
    }

    public static MediaInfo Create(
        string? secureUrl = null, 
        string? fileName = null,
        long? bytes = null,
        string? format = null,
        int? width = null,
        int? height = null,
        double? durationSeconds = null)
        => new(
            secureUrl, 
            fileName, 
            bytes,
            format,
            width,
            height,
            durationSeconds);

    public bool IsVideo => DurationSeconds.HasValue;
    public bool IsImage => Width.HasValue && Height.HasValue && !DurationSeconds.HasValue;

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return SecureUrl ?? string.Empty;
        yield return FileName ?? string.Empty;
        yield return Bytes ?? 0;
        yield return Format ?? string.Empty;
        yield return Width ?? 0;
        yield return Height ?? 0;
        yield return DurationSeconds ?? 0;
    }
}