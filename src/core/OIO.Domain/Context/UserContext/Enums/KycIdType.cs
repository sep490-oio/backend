using CSharpFunctionalExtensions;

namespace OIO.Domain.Context.UserContext.Enums;

public sealed class KycIdType : EnumValueObject<KycIdType>
{
    public static readonly KycIdType Cccd = new("cccd");
    public static readonly KycIdType Cmnd = new("cmnd");
    public static readonly KycIdType Passport = new("passport");
        
    public KycIdType(string id) : base(id)
    {
    }
}