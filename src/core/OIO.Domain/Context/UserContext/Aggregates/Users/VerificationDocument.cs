using OIO.Domain.Context.Shared.ValueObjects;
using OIO.Domain.Context.UserContext.Enums;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Entities;

namespace OIO.Domain.Context.UserContext.Aggregates.Users;

public sealed class VerificationDocument : BaseEntity<VerificationDocumentId>, ICreatedAtEntity
{
    public IdentityVerificationId VerificationId { get; private set; }
    public VerificationDocumentType DocumentType { get; private set; }
    public string ResourceType { get; private set; }
    public StorageRef StorageRef { get; private set; }
    public MediaInfo Info { get; private set; }
    public string? FileHash { get; private set; }
    public string? MimeType { get; private set; }
    public DocumentVerificationStatus VerificationStatus { get; private set; }
    public string? VerificationNotes { get; private set; }
    public string? ExtractedData { get; private set; } // jsonb
    public DateTime UploadedAt { get; private set; }
    public DateTime? VerifiedAt { get; private set; }
    public DateTime CreatedAt { get; private set; }

    // Navigation
    public IdentityVerification Verification { get; private set; } = null!;

    private VerificationDocument() { }
}