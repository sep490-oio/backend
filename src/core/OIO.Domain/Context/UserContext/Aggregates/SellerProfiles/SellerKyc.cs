using OIO.Domain.Context.UserContext.Enums;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Entities;

namespace OIO.Domain.Context.UserContext.Aggregates.SellerProfiles;

public sealed class SellerKyc : BaseEntity<SellerKycId>, IAuditableEntity
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