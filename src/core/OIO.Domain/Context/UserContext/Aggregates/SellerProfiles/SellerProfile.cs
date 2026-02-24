using System.Net;
using OIO.Domain.Context.UserContext.Enums;
using OIO.Domain.Context.UserContext.ValueObjects;
using OIO.Domain.SeedWork.Entities;

namespace OIO.Domain.Context.UserContext.Aggregates.SellerProfiles;

public sealed class SellerProfile : AggregateRoot<SellerProfileId>, IAuditableEntity
{
    private SellerProfile() {}
    
    public string StoreName { get; private set; }

    public string StoreDescription { get; private set; }

    public SellerProfileStatus Status { get; private set; }

    public DateTime? VerifiedAt { get; private set; }

    public int TotalSalesCount { get; private set; }

    public decimal TotalSalesAmount { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public DateTime? ModifiedAt { get; private set; }
}

public sealed class SellerKyc : Entity<SellerKycId>, IAuditableEntity
{
    private SellerKyc() {}
    
    public SellerProfileId SellerProfileId { get; private set; }

    public string FullName { get; private set; }

    public DateOnly DateOfBirth { get; private set; }

    public Gender? Gender { get; private set; }

    public string? Nationality { get; private set; }

    public KycIdType IdType { get; private set; }

    public string IdNumber { get; private set; }

    public DateOnly? IdIssuedDate { get; private set; }

    public DateOnly? IdExpiredDate { get; private set; }

    public string? IdIssuedPlace { get; private set; }

    public string? PermanentAddress { get; private set; }

    public string? Province { get; private set; }

    public string? District { get; private set; }

    public string? Ward { get; private set; }

    public SellerKycStatus Status { get; private set; }

    public DateTime? VerifiedAt { get; private set; }

    public Guid? VerifiedBy { get; private set; }

    public string? RejectionReason { get; private set; }

    public string? RejectionCode { get; private set; }

    public bool? AutoVerified { get; private set; }

    public decimal? AutoVerifyScore { get; private set; }

    public string? AutoVerifyProvider { get; private set; }

    public string? AutoVerifyResponse { get; private set; }

    public DateTime? SubmittedAt { get; private set; }

    public DateTime? ExpiresAt { get; private set; }

    public int? AttemptCount { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public DateTime? ModifiedAt { get; private set; }
}

public sealed class SellerKycDocument : Entity<SellerKycDocumentId>, ICreatedAtEntity
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

public sealed class SellerKycHistory : Entity<SellerKycHistoryId>
{
    private SellerKycHistory() {}
    
    public SellerKycId KycId { get; private set; }

    public SellerKycHistoryAction Action { get; private set; }

    public SellerKycStatus? OldStatus { get; private set; }

    public SellerKycStatus? NewStatus { get; private set; }

    public string? ChangedFields { get; private set; }

    public string? Notes { get; private set; }

    public UserId? PerformedBy { get; private set; }

    public KycHistoryPerformedByType? PerformedByType { get; private set; }

    public IPAddress? IpAddress { get; private set; }

    public string? UserAgent { get; private set; }

    public DateTime CreatedAt { get; private set; }
}