using CSharpFunctionalExtensions;

namespace OIO.Domain.Context.UserContext.Enums;

public sealed class IdentityVerificationStatus : EnumValueObject<IdentityVerificationStatus>
{
    public static readonly IdentityVerificationStatus Pending = new("pending");
    public static readonly IdentityVerificationStatus Submitted = new("submitted");
    public static readonly IdentityVerificationStatus UnderReview = new("under_review");
    public static readonly IdentityVerificationStatus Approved = new("approved");
    public static readonly IdentityVerificationStatus Rejected = new("rejected");
    public static readonly IdentityVerificationStatus Expired = new("expired");
    public static readonly IdentityVerificationStatus Suspended = new("suspended");
    private IdentityVerificationStatus(string id) : base(id) { }
}