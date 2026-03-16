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
    public string? EntityId { get; private set; }
    public string? IdType { get; private set; }
    public StorageRef StorageRef { get; private set; }
    public MediaInfo Info { get; private set; }
    public bool IsConfirmed { get; private set; }
    public bool IsLinked { get; private set; }
    public int RelocationAttemptCount { get; private set; }
    public DateTime? NextRelocationAttemptAt { get; private set; }
    public string? LastRelocationError { get; private set; }
    public DateTime? RelocatedAt { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public DateTime? ConfirmedAt { get; private set; }
    public DateTime? LinkedAt { get; private set; }


    private MediaUpload() { }

    public static MediaUpload Create(
        UserId userId,
        string context,
        string resourceType,
        string? entityId,
        string? idType,
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
            IdType = idType,
            Info = mediaInfo,
            StorageRef = storageRef,
            IsConfirmed = false,
            IsLinked = false,
            RelocationAttemptCount = 0,
            CreatedAt = nowUtc,
            ExpiresAt = nowUtc.Add(signatureExpirationMinutes)
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

    public UnitResult<Error> LinkToEntity<TId>(
        TId entityId,
        DateTime nowUtc) where TId  : IEntityId
    {
        if (!IsConfirmed)
            return Error.Conflict("Media.LinkUnConfirmed", "Cannot link unconfirmed upload.");

        switch (IsLinked)
        {
            case true when 
                string.Equals(EntityId, $"{entityId}", StringComparison.OrdinalIgnoreCase) &&
                string.Equals(IdType, typeof(TId).Name, StringComparison.OrdinalIgnoreCase):
                return UnitResult.Success<Error>();
            case true:
                return Error.Conflict("Media.AlreadyLinked", "Upload already linked.");
        }

        EntityId = $"{entityId}";
        IdType = typeof(TId).Name;
        IsLinked = true;
        LinkedAt = nowUtc;
        
        return UnitResult.Success<Error>();
    }

    public bool RequiresRelocation() =>
        IsLinked &&
        RelocatedAt is null &&
        !string.IsNullOrWhiteSpace(StorageRef.Folder) &&
        StorageRef.Folder.Contains("/pending/", StringComparison.OrdinalIgnoreCase);

    public UnitResult<Error> MarkRelocationSucceeded(
        StorageRef storageRef,
        MediaInfo mediaInfo,
        DateTime nowUtc)
    {
        if (!IsLinked)
            return Error.Conflict("Media.RelocationRequiresLink", "Cannot relocate media that is not linked.");

        StorageRef = storageRef;
        Info = mediaInfo;
        RelocatedAt = nowUtc;
        RelocationAttemptCount = 0;
        NextRelocationAttemptAt = null;
        LastRelocationError = null;

        return UnitResult.Success<Error>();
    }

    public UnitResult<Error> MarkRelocationRetry(
        string error,
        DateTime? nextAttemptAt)
    {
        if (!IsLinked)
            return Error.Conflict("Media.RelocationRequiresLink", "Cannot retry relocation for media that is not linked.");

        RelocationAttemptCount++;
        NextRelocationAttemptAt = nextAttemptAt;
        LastRelocationError = error;

        return UnitResult.Success<Error>();
    }
}
