using CSharpFunctionalExtensions;

namespace OIO.Domain.Context.UserContext.Enums;

public sealed class KycVerificationStatus : EnumValueObject<KycVerificationStatus>
{
    public static KycVerificationStatus Pending = new("pending");
    public static KycVerificationStatus Valid = new("valid");
    public static KycVerificationStatus Invalid = new("invalid");
    public static KycVerificationStatus Unclear = new("unclear");
    
    public KycVerificationStatus(string id) : base(id) {}
}