using CSharpFunctionalExtensions;
using OIO.Domain.Context.Shared.ValueObjects;
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
    public StorageRef StorageRef { get; private set; }
    public MediaInfo Info { get; private set; }
    public bool IsConfirmed { get; private set; }
    public bool IsLinked { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public DateTime? ConfirmedAt { get; private set; }
    public DateTime? LinkedAt { get; private set; }


    private MediaUpload() { }

    public static MediaUpload Create(
        UserId userId,
        string context,
        string resourceType,
        Guid? entityId,
        MediaInfo mediaInfo,
        StorageRef storageRef,
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
            Info = mediaInfo,
            StorageRef = storageRef,
            IsConfirmed = false,
            IsLinked = false,
            CreatedAt = nowUtc,
            ExpiresAt = nowUtc.Add(signatureExpirationMinutes)
        };
    }
    /// <summary>
    /// Creates a MediaUpload record that is pre-confirmed and ready to link.
    /// Used for server-side uploads (e.g. warehouse staff inspection photos)
    /// where the 3-step signature/upload/confirm cycle is bypassed.
    /// </summary>
    public static MediaUpload CreateServerSide(
        UserId userId,
        string context,
        string resourceType,
        MediaInfo mediaInfo,
        StorageRef storageRef,
        DateTime nowUtc)
    {
        return new MediaUpload
        {
            Id          = MediaUploadId.From(Guid.CreateVersion7()),
            UserId      = userId,
            Context     = context,
            ResourceType = resourceType,
            EntityId    = null,
            Info        = mediaInfo,
            StorageRef  = storageRef,
            IsConfirmed = true,
            IsLinked    = false,
            CreatedAt   = nowUtc,
            ConfirmedAt = nowUtc,
            ExpiresAt   = nowUtc.AddDays(30) // long expiry — will be linked immediately
        };
    }
    public bool IsExpired(DateTime nowUtc) => !IsConfirmed && nowUtc > ExpiresAt;

    public UnitResult<Error> Confirm(
        MediaInfo mediaInfo,
        TimeSpan orphanExpirationMinutes,
        DateTime nowUtc)
    {
        if (IsConfirmed)
            return Error.Conflict("Media.AlreadyConfirmed", "Upload already confirmed.");

        if (IsExpired(nowUtc))
            return Error.Conflict("Media.SignatureExpired", "Upload signature has expired.");

        Info = mediaInfo;
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