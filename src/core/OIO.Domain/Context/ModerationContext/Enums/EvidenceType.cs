using CSharpFunctionalExtensions;

namespace OIO.Domain.Context.ModerationContext.Enums;

public sealed class EvidenceType : EnumValueObject<EvidenceType>
{
    public static readonly EvidenceType Image = new("image");
    public static readonly EvidenceType Video = new("video");
    public static readonly EvidenceType Document = new("document");
    public static readonly EvidenceType Screenshot = new("screenshot");
    public static readonly EvidenceType Receipt = new("receipt");
    public static readonly EvidenceType Tracking = new("tracking");
    public static readonly EvidenceType Other = new("other");
    private EvidenceType(string id) : base(id) { }
}