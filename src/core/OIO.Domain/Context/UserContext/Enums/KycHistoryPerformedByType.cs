using CSharpFunctionalExtensions;

namespace OIO.Domain.Context.UserContext.Enums;

public sealed class KycHistoryPerformedByType : EnumValueObject<KycHistoryPerformedByType>
{
    public static readonly KycHistoryPerformedByType Admin = new("admin");
    public static readonly KycHistoryPerformedByType System = new("system");
    public static readonly KycHistoryPerformedByType Seller =  new("seller");
    
    public KycHistoryPerformedByType(string id) : base(id) {}
}