using OIO.Domain.Context.ModerationContext.Enums;
using OIO.Domain.Context.ModerationContext.ValueObjects.Ids;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Entities;

namespace OIO.Domain.Context.ModerationContext.Aggregates;

public sealed class ReviewQueue : BaseEntity<ReviewQueueId>, ICreatedAtEntity
{
    public string EntityType { get; private set; }
    public Guid EntityId { get; private set; }
    public decimal PriorityScore { get; private set; }
    public UserId? AssignedTo { get; private set; }
    public ReviewQueueStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private ReviewQueue() { }
}