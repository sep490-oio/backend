using System;
using System.Collections.Generic;

namespace OIO.Infrastructure.Databases.DataContext;

public partial class SellerKyc
{
    public Guid Id { get; set; }

    public Guid SellerProfileId { get; set; }

    public string FullName { get; set; } = null!;

    public DateOnly DateOfBirth { get; set; }

    public string? Gender { get; set; }

    public string? Nationality { get; set; }

    public string IdType { get; set; } = null!;

    public string IdNumber { get; set; } = null!;

    public DateOnly? IdIssuedDate { get; set; }

    public DateOnly? IdExpiredDate { get; set; }

    public string? IdIssuedPlace { get; set; }

    public string? PermanentAddress { get; set; }

    public string? Province { get; set; }

    public string? District { get; set; }

    public string? Ward { get; set; }

    public string Status { get; set; } = null!;

    public DateTime? VerifiedAt { get; set; }

    public Guid? VerifiedBy { get; set; }

    public string? RejectionReason { get; set; }

    public string? RejectionCode { get; set; }

    public bool? AutoVerified { get; set; }

    public decimal? AutoVerifyScore { get; set; }

    public string? AutoVerifyProvider { get; set; }

    public string? AutoVerifyResponse { get; set; }

    public DateTime? SubmittedAt { get; set; }

    public DateTime? ExpiresAt { get; set; }

    public int? AttemptCount { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? ModifiedAt { get; set; }

    public virtual ICollection<SellerKycDocument> SellerKycDocuments { get; set; } = new List<SellerKycDocument>();

    public virtual ICollection<SellerKycHistory> SellerKycHistories { get; set; } = new List<SellerKycHistory>();

    public virtual SellerProfile SellerProfile { get; set; } = null!;

    public virtual User? VerifiedByNavigation { get; set; }
}
