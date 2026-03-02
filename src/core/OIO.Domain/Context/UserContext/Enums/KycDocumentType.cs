using CSharpFunctionalExtensions;

namespace OIO.Domain.Context.UserContext.Enums;

public sealed class KycDocumentType : EnumValueObject<KycDocumentType>
{
    public static KycDocumentType IdFront = new ("id_front");
    public static KycDocumentType IdBack = new ("id_back");
    public static KycDocumentType Selfie = new ("selfie");
    public static KycDocumentType SelfieWithId = new ("selfie_with_id");
    public static KycDocumentType BusinessLicense = new ("business_license");
    public static KycDocumentType BankStatement = new ("bank_statement");
    public static KycDocumentType Other = new ("other");
    
    public KycDocumentType(string id) : base(id)
    {
    }
}