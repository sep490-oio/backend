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
    private readonly IAppConfigs _appConfigs;

    public UploadContextRegistry(IAppConfigs appConfigs)
    {
        _appConfigs = appConfigs;
    }

    // ==================== Context Lookup ====================

    public async Task<UploadContextOption?> GetAsync(string contextName, CancellationToken cancellationToken = default)
    {
        var contexts = await _appConfigs.Media.GetUploadContextsAsync(cancellationToken);
        return contexts.FirstOrDefault(c => c.Name.Equals(contextName, StringComparison.OrdinalIgnoreCase));
    }


    public async Task<string[]> GetAllContextAsync(CancellationToken cancellationToken = default)
    {
        var contexts = await _appConfigs.Media.GetUploadContextsAsync(cancellationToken);
        return contexts.Select(x => x.Name).ToArray();
    }

    public async Task<bool> IsValidAsync(string contextName, CancellationToken cancellationToken = default)
    {
        var contexts = await _appConfigs.Media.GetUploadContextsAsync(cancellationToken);
        return contexts.Any(c => c.Name.Equals(contextName, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<UnitResult<Error>> ValidateContextAsync(string contextName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(contextName))
            return Error.NotEmpty(contextName, false, "Media", "Context");

        if (! await IsValidAsync(contextName, cancellationToken))
            return Error.InSet(contextName, string.Join(", ", await GetAllContextAsync(cancellationToken)), false,"Media", "Context");

        return UnitResult.Success<Error>();
    }

    // ==================== Entity Context Classification ====================

    public bool IsItemContext(string contextName) =>
        contextName.StartsWith("item_", StringComparison.OrdinalIgnoreCase);

    public bool IsUserContext(string contextName) =>
        contextName.StartsWith("user_", StringComparison.OrdinalIgnoreCase);
    
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
    
    // ==================== Resource Type Limits ====================

    public async Task<int> GetMaxUploadsForContextAsync(string contextName)
    {
        var context = await GetAsync(contextName);
        return context?.MaxUploadsPerEntity ?? 10;
    }
    
    public async Task<int> GetMaxForEntityMediaAsync(
        string entityPrefix, 
        string resourceType,
        CancellationToken cancellationToken = default)
    {
        var allContext = await _appConfigs.Media.GetUploadContextsAsync(cancellationToken);
        
        var context = allContext.FirstOrDefault(c =>
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
