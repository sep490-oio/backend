using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Media;
using OIO.Infrastructure.Settings;
namespace OIO.Infrastructure.Media;

internal sealed class CloudinarySignatureService : IMediaSignatureService
{
    private readonly IOptionsMonitor<CloudinaryOptions> _optionsMonitor;
    private readonly ILogger<CloudinarySignatureService> _logger;
    private readonly IClock _clock;
    private CloudinaryOptions Options => _optionsMonitor.CurrentValue;

    public CloudinarySignatureService(
        IOptionsMonitor<CloudinaryOptions> optionsMonitor,
        ILogger<CloudinarySignatureService> logger,
        IClock clock)
    {
        _optionsMonitor = optionsMonitor;
        _logger = logger;
        _clock = clock;
         _logger.LogInformation("CloudinarySignatureService initialized with CloudName: {CloudName}",
            Options.CloudName);
    }
    
    private Cloudinary CreateClient()
    {
        var opts = Options;
        var account = new Account(opts.CloudName, opts.ApiKey, opts.ApiSecret);
        return new Cloudinary(account);
    }

    public UploadSignatureResult GenerateSignature(
        MediaResourceType resourceType,
        string mediaName,
        string folder,
        string? eager = null,
        string[]? allowedFormats = null)
    {
        var opts = Options;
        var timestamp = new DateTimeOffset(_clock.UtcNow).ToUnixTimeSeconds();
        var resourceTypePath = resourceType.ToCloudinaryPath();

        // Build parameters to sign (alphabetical order)
        var paramsToSign = new SortedDictionary<string, string>
        {
            ["folder"] = folder,
            ["public_id"] = mediaName,
            ["timestamp"] = timestamp.ToString(CultureInfo.InvariantCulture)
        };

        if (!string.IsNullOrEmpty(eager))
            paramsToSign["eager"] = eager;

        if (allowedFormats is { Length: > 0 })
            paramsToSign["allowed_formats"] = string.Join(",", allowedFormats);

        // Build resource_type specific params
        if (resourceType == MediaResourceType.Video)
        {
            // Video-specific: request async eager transformations
            paramsToSign["eager_async"] = "true";
        }

        // Create string to sign + append API secret
        var stringToSign = string.Join("&",
            paramsToSign.Select(kvp => $"{kvp.Key}={kvp.Value}"));
        stringToSign += opts.ApiSecret;

        var signature = ComputeSha1(stringToSign);

        // Upload URL varies by resource type
        var uploadUrl = $"{opts.BaseUrl}/{opts.CloudName}/{resourceTypePath}/upload";
        var publicId = $"{folder}/{mediaName}";

        _logger.LogDebug(
            "Generated {ResourceType} upload signature: publicId={PublicId}, folder={Folder}",
            resourceTypePath, publicId, folder);

        return new UploadSignatureResult(
            UploadUrl: uploadUrl,
            Signature: signature,
            Timestamp: timestamp,
            ApiKey: opts.ApiKey,
            CloudName: opts.CloudName,
            PublicId: publicId,
            Folder: folder,
            Eager: eager,
            ResourceType: resourceTypePath);
    }

    public async Task<bool> DeleteResourceAsync(
        string publicId,
        MediaResourceType resourceType,
        CancellationToken ct = default)
    {
        try
        {
            var cloudinary = CreateClient();
            
            var deletionParams = new DeletionParams(publicId)
            {
                ResourceType = MapResourceType(resourceType)
            };

            var result = await cloudinary.DestroyAsync(deletionParams);

            if (result.Error is null)
            {
                _logger.LogInformation(
                    "Deleted {Type} resource: {PublicId}, Result: {Result}",
                    resourceType, publicId, result.Result);
                return true;
            }

            _logger.LogWarning(
                "Failed to delete {Type} resource: {PublicId}, Error: {Error}",
                resourceType, publicId, result.Error.Message);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Exception deleting {Type} resource: {PublicId}",
                resourceType, publicId);
            return false;
        }
    }

    public async Task<int> DeleteResourcesAsync(
        IEnumerable<(string PublicId, MediaResourceType ResourceType)> resources,
        CancellationToken ct = default)
    {
        var cloudinary = CreateClient();
        var deleted = 0;

        // Group by resource type for batch deletion
        var grouped = resources.GroupBy(r => r.ResourceType);

        foreach (var group in grouped)
        {
            var publicIds = group.Select(r => r.PublicId).ToList();
            var cloudinaryType = MapResourceType(group.Key);

            try
            {
                var delParams = new DelResParams
                {
                    PublicIds = publicIds,
                    ResourceType = cloudinaryType
                };

                var result = await cloudinary.DeleteResourcesAsync(delParams);

                if (result.Error is null)
                {
                    var batchDeleted = result.Deleted?.Count(kvp =>
                        kvp.Value == "deleted") ?? 0;
                    deleted += batchDeleted;

                    _logger.LogInformation(
                        "Batch deleted {Count}/{Total} {Type} resources.",
                        batchDeleted, publicIds.Count, group.Key);
                }
                else
                {
                    _logger.LogWarning(
                        "Batch delete failed for {Type}: {Error}",
                        group.Key, result.Error.Message);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Exception in batch delete for {Type}, {Count} resources.",
                    group.Key, publicIds.Count);
            }
        }

        return deleted;
    }

    // ==================== Helpers ====================

    private static ResourceType MapResourceType(MediaResourceType type) => type switch
    {
        MediaResourceType.Image => ResourceType.Image,
        MediaResourceType.Video => ResourceType.Video,
        MediaResourceType.Raw => ResourceType.Raw,
        _ => ResourceType.Image
    };

    private static string ComputeSha1(string input)
    {
        var bytes = SHA1.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexStringLower(bytes);
    }
}