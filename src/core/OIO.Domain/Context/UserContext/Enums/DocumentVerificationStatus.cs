using CSharpFunctionalExtensions;

namespace OIO.Domain.Context.UserContext.Enums;

public sealed class DocumentVerificationStatus : EnumValueObject<DocumentVerificationStatus>
{
    public static readonly DocumentVerificationStatus Pending = new("pending");
    public static readonly DocumentVerificationStatus Valid = new("valid");
    public static readonly DocumentVerificationStatus Invalid = new("invalid");
    public static readonly DocumentVerificationStatus Unclear = new("unclear");
    private DocumentVerificationStatus(string id) : base(id) { }
}