using CSharpFunctionalExtensions;

namespace OIO.Domain.Context.UserContext.Enums;

public sealed class SellerProfileStatus : EnumValueObject<SellerProfileStatus>
{
    public static readonly SellerProfileStatus Pending =  new("pending");
    public static readonly SellerProfileStatus Verified = new("verified");
    public static readonly SellerProfileStatus Rejected = new("rejected");
    
    public SellerProfileStatus(string id) : base(id)
    {
    }
}