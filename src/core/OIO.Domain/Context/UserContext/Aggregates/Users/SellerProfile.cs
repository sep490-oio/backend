using System.Net;
using OIO.Domain.Context.UserContext.Enums;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Entities;

namespace OIO.Domain.Context.UserContext.Aggregates.Users;

public sealed class SellerProfile : BaseEntity<UserId>, IAuditableEntity
{
    public string StoreName { get; private set; }
    public string StoreDescription { get; private set; }
    public SellerProfileStatus Status { get; private set; }
    public DateTime? VerifiedAt { get; private set; }
    public int TotalSalesCount { get; private set; }
    public decimal TotalSalesAmount { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? ModifiedAt { get; private set; }

    // Navigation
    public User User { get; private set; } = null!;

    private SellerProfile() { }
}

/// <summary>
/// Maps to: user_identity_verification_history
/// </summary>
public sealed class VerificationHistory : BaseEntity<VerificationHistoryId>, ICreatedAtEntity
{
    public IdentityVerificationId VerificationId { get; private set; }
    public VerificationHistoryAction Action { get; private set; }
    public string? OldStatus { get; private set; }
    public string? NewStatus { get; private set; }
    public string? ChangedFields { get; private set; } // jsonb
    public string? Notes { get; private set; }
    public UserId? PerformedBy { get; private set; }
    public PerformerType PerformedByType { get; private set; }
    public IPAddress? IpAddress { get; private set; }  // inet → string
    public string? UserAgent { get; private set; }
    public DateTime CreatedAt { get; private set; }

    // Navigation
    public IdentityVerification Verification { get; private set; } = null!;

    private VerificationHistory() { }
}