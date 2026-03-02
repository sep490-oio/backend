using CSharpFunctionalExtensions;

namespace OIO.Domain.Context.UserContext.Enums;

public sealed class SellerKycStatus : EnumValueObject<SellerKycStatus>
{
    public static readonly SellerKycStatus Pending = new("pending");
    public static readonly SellerKycStatus Submitted = new("submitted");
    public static readonly SellerKycStatus UnderReview = new("under_review");
    public static readonly SellerKycStatus Approved = new("approved");
    public static readonly SellerKycStatus Rejected = new("rejected");
    public static readonly SellerKycStatus Expired = new("expired");
    public static readonly SellerKycStatus Suspended = new("suspended");
  
    public SellerKycStatus(string id) : base(id)
    {
    }
}