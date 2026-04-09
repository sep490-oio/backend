using OIO.Domain.Context.ModerationContext.ValueObjects.Ids;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Entities;

namespace OIO.Domain.Context.ModerationContext.Aggregates.Disputes;

public sealed class DisputeFinding : BaseEntity<DisputeFindingId>, ICreatedAtEntity
{
    private readonly List<DisputeFindingReference> _references = [];

    public DisputeId DisputeId { get; private set; }
    public string Domain { get; private set; }
    public UserId AuthorUserId { get; private set; }
    public string? VerdictRecommendation { get; private set; }
    public string Summary { get; private set; }
    public string? FindingNote { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public Dispute Dispute { get; private set; } = null!;
    public IReadOnlyCollection<DisputeFindingReference> References => _references.AsReadOnly();

    private DisputeFinding() { }

    public static DisputeFinding Create(
        DisputeId disputeId,
        string domain,
        UserId authorUserId,
        string summary,
        DateTime nowUtc,
        string? verdictRecommendation = null,
        string? findingNote = null)
    {
        return new DisputeFinding
        {
            Id = DisputeFindingId.From(Guid.CreateVersion7()),
            DisputeId = disputeId,
            Domain = domain,
            AuthorUserId = authorUserId,
            VerdictRecommendation = verdictRecommendation,
            Summary = summary,
            FindingNote = findingNote,
            CreatedAt = nowUtc
        };
    }

    public void AddReference(string referenceType, Guid targetId, string labelSnapshot, DateTime now)
    {
        _references.Add(DisputeFindingReference.Create(Id, referenceType, targetId, labelSnapshot, now));
    }
}
