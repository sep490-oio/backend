using OIO.Domain.Context.UserContext.Enums;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Entities;

namespace OIO.Domain.Context.UserContext.Aggregates.SellerProfiles;

public sealed class SellerKycDocument : BaseEntity<SellerKycDocumentId>, ICreatedAtEntity
{
    private SellerKycDocument() {}
    
    public SellerKycId KycId { get; private set; }

    public KycDocumentType DocumentType { get; private set; } 

    public string FileUrl { get; private set; }

    public string? FileName { get; private set; }

    public int? FileSize { get; private set; }

    public string? FileHash { get; private set; }

    public string? MimeType { get; private set; }

    public KycVerificationStatus? VerificationStatus { get; private set; }

    public string? VerificationNotes { get; private set; }

    public string? ExtractedData { get; private set; }

    public DateTime UploadedAt { get; private set; }

    public DateTime? VerifiedAt { get; private set; }

    public DateTime CreatedAt { get; private set; }
}