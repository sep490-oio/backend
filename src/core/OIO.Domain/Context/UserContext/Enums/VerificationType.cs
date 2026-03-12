using CSharpFunctionalExtensions;

namespace OIO.Domain.Context.UserContext.Enums;

public sealed class VerificationType : EnumValueObject<VerificationType>
{
    public static readonly VerificationType GovernmentId = new("government_id");
    public static readonly VerificationType Passport = new("passport");
    public static readonly VerificationType BusinessOwner = new("business_owner");
    public static readonly VerificationType Manual = new("manual");
    private VerificationType(string id) : base(id) { }
}