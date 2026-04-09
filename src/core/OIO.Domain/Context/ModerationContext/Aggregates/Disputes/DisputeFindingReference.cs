using OIO.Domain.Context.ModerationContext.ValueObjects.Ids;

namespace OIO.Domain.Context.ModerationContext.Aggregates.Disputes;

public sealed class DisputeFindingReference
{
    public Guid Id { get; private set; }
    public DisputeFindingId FindingId { get; private set; }
    public string ReferenceType { get; private set; } = null!;
    public Guid TargetId { get; private set; }
    public string LabelSnapshot { get; private set; } = null!;
    public DateTime CreatedAt { get; private set; }

    private DisputeFindingReference() { }

    public static DisputeFindingReference Create(
        DisputeFindingId findingId,
        string referenceType,
        Guid targetId,
        string labelSnapshot,
        DateTime now)
    {
        return new DisputeFindingReference
        {
            Id = Guid.CreateVersion7(),
            FindingId = findingId,
            ReferenceType = referenceType,
            TargetId = targetId,
            LabelSnapshot = labelSnapshot,
            CreatedAt = now
        };
    }
}
