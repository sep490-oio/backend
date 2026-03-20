using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using CSharpFunctionalExtensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Media;
using OIO.Domain.SeedWork.Errors;
using OIO.Infrastructure.Settings;
using Error = OIO.Domain.SeedWork.Errors.Error;

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
        string publicId,
        string folder,
        string? eager = null,
        string[]? allowedFormats = null)
    {
        var opts = Options;
        var timestamp = new DateTimeOffset(_clock.UtcNow).ToUnixTimeSeconds();
        var resourceTypePath = resourceType.ToCloudinaryPath();

        var paramsToSign = new SortedDictionary<string, string>
        {
            ["folder"]    = folder,
            ["public_id"] = publicId,
            ["timestamp"] = timestamp.ToString(CultureInfo.InvariantCulture)
        };

        if (!string.IsNullOrEmpty(eager))
            paramsToSign["eager"] = eager;

        if (allowedFormats is { Length: > 0 })
            paramsToSign["allowed_formats"] = string.Join(",", allowedFormats);

        if (resourceType == MediaResourceType.Video)
            paramsToSign["eager_async"] = "true";

        var stringToSign = string.Join("&",
            paramsToSign.Select(kvp => $"{kvp.Key}={kvp.Value}"));
        stringToSign += opts.ApiSecret;

        var signature = ComputeSha1(stringToSign);
        var uploadUrl = $"{opts.BaseUrl}/{opts.CloudName}/{resourceTypePath}/upload";

        _logger.LogDebug(
            "Generated {ResourceType} upload signature: publicId={PublicId}, folder={Folder}",
            resourceTypePath, publicId, folder);

        return new UploadSignatureResult(
            UploadUrl:    uploadUrl,
            Signature:    signature,
            Timestamp:    timestamp,
            ApiKey:       opts.ApiKey,
            CloudName:    opts.CloudName,
            PublicId:     publicId,
            Folder:       folder,
            Eager:        eager,
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
                _logger.LogInformation("Deleted {Type} resource: {PublicId}, Result: {Result}",
                    resourceType, publicId, result.Result);
                return true;
            }
            _logger.LogWarning("Failed to delete {Type} resource: {PublicId}, Error: {Error}",
                resourceType, publicId, result.Error.Message);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception deleting {Type} resource: {PublicId}", resourceType, publicId);
            return false;
        }
    }

    public async Task<int> DeleteResourcesAsync(
        IEnumerable<(string PublicId, MediaResourceType ResourceType)> resources,
        CancellationToken ct = default)
    {
        var cloudinary = CreateClient();
        var deleted = 0;

        foreach (var group in resources.GroupBy(r => r.ResourceType))
        {
            var publicIds      = group.Select(r => r.PublicId).ToList();
            var cloudinaryType = MapResourceType(group.Key);
            try
            {
                var result = await cloudinary.DeleteResourcesAsync(new DelResParams
                {
                    PublicIds    = publicIds,
                    ResourceType = cloudinaryType
                });
                if (result.Error is null)
                {
                    var batchDeleted = result.Deleted?.Count(kvp => kvp.Value == "deleted") ?? 0;
                    deleted += batchDeleted;
                    _logger.LogInformation("Batch deleted {Count}/{Total} {Type} resources.",
                        batchDeleted, publicIds.Count, group.Key);
                }
                else
                {
                    _logger.LogWarning("Batch delete failed for {Type}: {Error}",
                        group.Key, result.Error.Message);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in batch delete for {Type}, {Count} resources.",
                    group.Key, publicIds.Count);
            }
        }

        return deleted;
    }

    private static ResourceType MapResourceType(MediaResourceType type) => type switch
    {
        MediaResourceType.Image => ResourceType.Image,
        MediaResourceType.Video => ResourceType.Video,
        MediaResourceType.Raw   => ResourceType.Raw,
        _                       => ResourceType.Image
    };

    private static string ComputeSha1(string input)
    {
        var bytes = SHA1.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexStringLower(bytes);
    }
}

internal sealed class CloudinaryDirectUploadService : IMediaDirectUploadService
{
    private readonly IOptionsMonitor<CloudinaryOptions> _optionsMonitor;
    private readonly ILogger<CloudinaryDirectUploadService> _logger;
    private CloudinaryOptions Options => _optionsMonitor.CurrentValue;

    public CloudinaryDirectUploadService(
        IOptionsMonitor<CloudinaryOptions> optionsMonitor,
        ILogger<CloudinaryDirectUploadService> logger)
    {
        _optionsMonitor = optionsMonitor;
        _logger = logger;
    }

    public async Task<Result<DirectUploadResult, Error>> UploadAsync(
        Stream fileStream,
        string fileName,
        string folder,
        string? eager = null,
        CancellationToken ct = default)
    {
        try
        {
            var opts       = Options;
            var account    = new Account(opts.CloudName, opts.ApiKey, opts.ApiSecret);
            var cloudinary = new Cloudinary(account);

            var uploadParams = new ImageUploadParams
            {
                File           = new FileDescription(fileName, fileStream),
                Folder         = folder,
                UseFilename    = false,
                UniqueFilename = true,
            };

            var result = await cloudinary.UploadAsync(uploadParams, ct);

            if (result.Error is not null)
            {
                _logger.LogWarning("Cloudinary direct upload failed: {Error}", result.Error.Message);
                return Error.Unexpected("CloudinaryUploadFailed", result.Error.Message);
            }

            _logger.LogInformation("Direct upload succeeded: PublicId={PublicId}", result.PublicId);

            return new DirectUploadResult(
                PublicId:  result.PublicId,
                SecureUrl: result.SecureUrl.ToString(),
                Bytes:     result.Bytes,
                Format:    result.Format,
                FileName:  fileName,
                Width:     result.Width,
                Height:    result.Height);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception during direct upload for file {FileName}", fileName);
            return Error.Unexpected("CloudinaryUploadException", ex.Message);
        }
    }
}