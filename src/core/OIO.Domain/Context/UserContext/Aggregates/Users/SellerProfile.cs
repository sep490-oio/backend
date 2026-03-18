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

    public UnitResult<Error> Suspend(DateTime nowUtc, string? reason = null)
    {
        if (Status == SellerProfileStatus.Suspended)
            return UnitResult.Success<Error>();

        Status = SellerProfileStatus.Suspended;
        ModifiedAt = nowUtc;

        return UnitResult.Success<Error>();
    }
}