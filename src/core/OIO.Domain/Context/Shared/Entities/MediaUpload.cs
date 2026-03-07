using CSharpFunctionalExtensions;
using OIO.Domain.Context.Shared.ValueObjects.Ids;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Entities;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Domain.Context.Shared.Entities;

public sealed class MediaUpload : BaseEntity<MediaUploadId>, ICreatedAtEntity
{
    public UserId UserId { get; private set; }
    public string Context { get; private set; } = null!;
    public string ResourceType { get; private set; } = null!;
    public Guid? EntityId { get; private set; }
    public string PublicId { get; private set; } = null!;
    public string Folder { get; private set; } = null!;
    public bool IsConfirmed { get; private set; }
    public bool IsLinked { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public DateTime? ConfirmedAt { get; private set; }
    public DateTime? LinkedAt { get; private set; }

    public string? SecureUrl { get; private set; }
    public string? FileName { get; private set; }
    public long? Bytes { get; private set; }
    public string? Format { get; private set; }
    public int? Width { get; private set; }
    public int? Height { get; private set; }
    public double? DurationSeconds { get; private set; }

    private MediaUpload() { }

    public static MediaUpload Create(
        UserId userId,
        string context,
        string resourceType,
        Guid? entityId,
        string publicId,
        string folder,
        string? fileName,
        TimeSpan signatureExpirationMinutes,
        DateTime nowUtc)
    {
        return new MediaUpload
        {
            Id = MediaUploadId.From(Guid.CreateVersion7()),
            UserId = userId,
            Context = context,
            ResourceType = resourceType,
            EntityId = entityId,
            PublicId = publicId,
            Folder = folder,
            FileName = fileName,
            IsConfirmed = false,
            IsLinked = false,
            CreatedAt = nowUtc,
            ExpiresAt = nowUtc.Add(signatureExpirationMinutes)
        };
    }

    public bool IsExpired(DateTime nowUtc) => !IsConfirmed && nowUtc > ExpiresAt;

    public UnitResult<Error> Confirm(
        string secureUrl,
        long bytes,
        string format,
        int? width,
        int? height,
        TimeSpan orphanExpirationMinutes,
        double? durationSeconds,
        DateTime nowUtc)
    {
        if (IsConfirmed)
            return Error.Conflict("Media.AlreadyConfirmed", "Upload already confirmed.");

        if (IsExpired(nowUtc))
            return Error.Conflict("Media.SignatureExpired", "Upload signature has expired.");

        SecureUrl = secureUrl;
        Bytes = bytes;
        Format = format;
        Width = width;
        Height = height;
        DurationSeconds = durationSeconds;
        IsConfirmed = true;
        ConfirmedAt = nowUtc;

        // Extend expiration to give user time to submit the form
        ExpiresAt = nowUtc.Add(orphanExpirationMinutes);
        
        return UnitResult.Success<Error>();
    }

    public UnitResult<Error> LinkToEntity(
        Guid entityId,
        DateTime nowUtc)
    {
        if (!IsConfirmed)
            return Error.Conflict("Media.LinkUnConfirmed", "Cannot link unconfirmed upload.");

        if (IsLinked)
            return Error.Conflict("Media.AlreadyLinked", "Upload already linked.");

        EntityId = entityId;
        IsLinked = true;
        LinkedAt = nowUtc;
        
        return UnitResult.Success<Error>();
    }
}