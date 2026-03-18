using CSharpFunctionalExtensions;

namespace OIO.Domain.Context.UserContext.Enums;

public sealed class VerificationDocumentType : EnumValueObject<VerificationDocumentType>
{
    public static readonly VerificationDocumentType IdFront = new("id_front");
    public static readonly VerificationDocumentType IdBack = new("id_back");
    public static readonly VerificationDocumentType Selfie = new("selfie");
    public static readonly VerificationDocumentType SelfieWithId = new("selfie_with_id");
    public static readonly VerificationDocumentType BusinessLicense = new("business_license");
    public static readonly VerificationDocumentType BankStatement = new("bank_statement");
    public static readonly VerificationDocumentType Other = new("other");
    private VerificationDocumentType(string id) : base(id) { }
}