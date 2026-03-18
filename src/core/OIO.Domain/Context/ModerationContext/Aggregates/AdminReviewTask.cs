using OIO.Domain.Context.ModerationContext.Enums;
using OIO.Domain.Context.ModerationContext.ValueObjects.Ids;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Entities;

namespace OIO.Domain.Context.ModerationContext.Aggregates;

public sealed class AdminReviewTask : BaseEntity<AdminReviewTaskId>, IAuditableEntity
{
    public string EntityType { get; private set; }
    public Guid EntityId { get; private set; }
    public UserId? AssignedTo { get; private set; }
    public AdminReviewTaskStatus Status { get; private set; }
    public DisputePriority Priority { get; private set; }  // reuse — same values
    public DateTime? DueAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? ModifiedAt { get; private set; }

    private AdminReviewTask() { }
}