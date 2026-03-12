using System.Net;
using CSharpFunctionalExtensions;
using OIO.Domain.Context.UserContext.Enums;
using OIO.Domain.Context.UserContext.Errors;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Entities;
using OIO.Domain.SeedWork.Errors;

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

    public static SellerProfile Create(
        UserId userId,
        string storeName,
        string storeDescription,
        DateTime nowUtc)
    {
        return new SellerProfile
        {
            Id = userId,
            StoreName = storeName.Trim(),
            StoreDescription = storeDescription.Trim(),
            Status = SellerProfileStatus.Pending,
            TotalSalesCount = 0,
            TotalSalesAmount = 0,
            CreatedAt = nowUtc
        };
    }

    public UnitResult<Error> Update(
        string storeName,
        string storeDescription,
        DateTime nowUtc)
    {
        if (Status == SellerProfileStatus.Rejected)
        {
            Status = SellerProfileStatus.Pending;
        }

        StoreName = storeName.Trim();
        StoreDescription = storeDescription.Trim();
        ModifiedAt = nowUtc;

        return UnitResult.Success<Error>();
    }

    public UnitResult<Error> Verify(DateTime nowUtc)
    {
        if (Status != SellerProfileStatus.Pending)
            return UserErrors.SellerProfile.CannotVerifyInCurrentStatus(Status);

        Status = SellerProfileStatus.Verified;
        VerifiedAt = nowUtc;
        ModifiedAt = nowUtc;

        return UnitResult.Success<Error>();
    }

    public UnitResult<Error> Reject(DateTime nowUtc)
    {
        if (Status != SellerProfileStatus.Pending)
            return UserErrors.SellerProfile.CannotRejectInCurrentStatus(Status);

        Status = SellerProfileStatus.Rejected;
        ModifiedAt = nowUtc;

        return UnitResult.Success<Error>();
    }
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

    internal static VerificationHistory Create(
        IdentityVerificationId verificationId,
        VerificationHistoryAction action,
        string? oldStatus,
        string? newStatus,
        UserId performedBy,
        PerformerType performerType,
        DateTime nowUtc,
        string? notes = null)
    {
        return new VerificationHistory
        {
            Id = VerificationHistoryId.From(Guid.CreateVersion7()),
            VerificationId = verificationId,
            Action = action,
            OldStatus = oldStatus,
            NewStatus = newStatus,
            PerformedBy = performedBy,
            PerformedByType = performerType,
            Notes = notes,
            CreatedAt = nowUtc
        };
    }
}