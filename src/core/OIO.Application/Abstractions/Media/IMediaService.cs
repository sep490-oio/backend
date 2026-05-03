using CSharpFunctionalExtensions;
using OIO.Application.Abstractions.Commons;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Abstractions.Media;

public interface IMediaSignatureService
{
    /// <summary>
    /// Generate signed upload parameters for image, video, or raw resources.
    /// </summary>
    UploadSignatureResult GenerateSignature(
        MediaResourceType resourceType,
        string mediaName,
        string folder,
        string? eager = null,
        string[]? allowedFormats = null);

    /// <summary>
    /// Rename an existing resource to a final public id/folder.
    /// </summary>
    Task<RenameResourceResult?> RenameResourceAsync(
        string fromPublicId,
        string toPublicId,
        MediaResourceType resourceType,
        CancellationToken ct = default);

    /// <summary>
    /// Delete a resource from Cloudinary by publicId and resource type.
    /// </summary>
    Task<bool> DeleteResourceAsync(
        string publicId,
        MediaResourceType resourceType,
        CancellationToken ct = default);

    /// <summary>
    /// Delete multiple resources in batch.
    /// </summary>
    Task<int> DeleteResourcesAsync(
        IEnumerable<(string PublicId, MediaResourceType ResourceType)> resources,
        CancellationToken ct = default);
}

/// <summary>
/// Server-side direct upload to Cloudinary — used for internal staff flows
/// where the client should not be responsible for the 3-step signature/upload/confirm cycle.
/// </summary>
public interface IMediaDirectUploadService
{
    /// <summary>
    /// Upload a raw file stream directly to Cloudinary from the server.
    /// Returns the resulting upload metadata.
    /// </summary>
    Task<Result<DirectUploadResult, Error>> UploadAsync(
        Stream fileStream,
        string fileName,
        string folder,
        string? eager = null,
        CancellationToken ct = default);
}

public sealed record DirectUploadResult(
    string PublicId,
    string SecureUrl,
    long Bytes,
    string Format,
    string FileName,
    int? Width,
    int? Height);

public sealed record UploadSignatureResult(
    string UploadUrl,
    string Signature,
    long Timestamp,
    string ApiKey,
    string CloudName,
    string UploadPublicId,
    string StoragePublicId,
    string Folder,
    string? Eager,
    string ResourceType);

public sealed record RenameResourceResult(
    string PublicId,
    string Folder,
    string SecureUrl);
 
public sealed class UploadContextRegistry
{
    private readonly IRuntimeSettings _runtimeSettings;

    public UploadContextRegistry(IRuntimeSettings runtimeSettings)
    {
        _runtimeSettings = runtimeSettings;
    }

    // ==================== Context Lookup ====================

    public UploadContextOption? Get(string contextName) =>
        _runtimeSettings.Media.UploadContexts
            .FirstOrDefault(c => c.Name.Equals(contextName, StringComparison.OrdinalIgnoreCase));


    public string[] GetAllContext() =>
        _runtimeSettings.Media.UploadContexts
            .Select(x => x.Name)
            .ToArray();

    public bool IsValid(string contextName) =>
        _runtimeSettings.Media.UploadContexts
            .Any(c => c.Name.Equals(contextName, StringComparison.OrdinalIgnoreCase));

    public UnitResult<Error> ValidateContext(string contextName)
    {
        if (string.IsNullOrWhiteSpace(contextName))
            return Error.NotEmpty(contextName, false, "Media", "Context");

        if (!IsValid(contextName))
            return Error.InSet(contextName, string.Join(", ", GetAllContext()), false, "Media", "Context");

        return UnitResult.Success<Error>();
    }

    // ==================== Entity Context Classification ====================

    public bool IsItemContext(string contextName) =>
        contextName.StartsWith("item_", StringComparison.OrdinalIgnoreCase);

    public bool IsUserContext(string contextName) =>
        contextName.StartsWith("user_", StringComparison.OrdinalIgnoreCase);

    public bool IsUserAvatarContext(string contextName) =>
        string.Equals(contextName, "user_avatar", StringComparison.OrdinalIgnoreCase);
    
    public bool IsTermContext(string contextName) =>
        contextName.StartsWith("term_", StringComparison.OrdinalIgnoreCase);

    public bool IsCategoryContext(string contextName) =>
        contextName.StartsWith("category_", StringComparison.OrdinalIgnoreCase);

    public bool IsDisputeContext(string contextName) =>
        contextName.StartsWith("dispute_", StringComparison.OrdinalIgnoreCase);

    public bool IsVerificationContext(string contextName) =>
        contextName.StartsWith("verification_", StringComparison.OrdinalIgnoreCase);

    public bool IsWarehouseInspectionContext(string contextName) =>
        contextName.StartsWith("warehouse_inspection_", StringComparison.OrdinalIgnoreCase);

    public bool IsShipmentContext(string contextName) =>
        contextName.StartsWith("shipment_", StringComparison.OrdinalIgnoreCase);

    // ==================== Resource Type Limits ====================

    public int GetMaxUploadsForContext(string contextName)
    {
        var context = Get(contextName);
        return context?.MaxUploadsPerEntity ?? 10;
    }
    
    public int GetMaxForEntityMedia(
        string entityPrefix,
        string resourceType)
    {
        var context = _runtimeSettings.Media.UploadContexts.FirstOrDefault(c =>
            c.Name.StartsWith($"{entityPrefix}_", StringComparison.OrdinalIgnoreCase) &&
            c.ResourceType.Equals(resourceType, StringComparison.OrdinalIgnoreCase));

        return context?.MaxUploadsPerEntity ?? 10;
    }

    // ==================== Resource Type Parsing ====================

    public static MediaResourceType ParseResourceType(string resourceType) =>
        resourceType.ToLowerInvariant() switch
        {
            "image" => MediaResourceType.Image,
            "video" => MediaResourceType.Video,
            "raw" => MediaResourceType.Raw,
            "document" => MediaResourceType.Raw,
            _ => throw new ArgumentException($"Unknown resource type: {resourceType}")
        };
}
    
public enum MediaResourceType
{
    Image,
    Video,
    Raw
}

public static class MediaResourceTypeExtensions
{
    public static string ToCloudinaryPath(this MediaResourceType type) => type switch
    {
        MediaResourceType.Image => "image",
        MediaResourceType.Video => "video",
        MediaResourceType.Raw => "raw",
        _ => throw new ArgumentOutOfRangeException(nameof(type))
    };

    public static string ToFilePrefix(this MediaResourceType type) => type switch
    {
        MediaResourceType.Image => "img",
        MediaResourceType.Video => "vid",
        MediaResourceType.Raw => "file",
        _ => "file"
    };
}
