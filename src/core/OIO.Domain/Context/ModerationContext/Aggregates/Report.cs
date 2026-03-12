using OIO.Domain.Context.ModerationContext.Enums;
using OIO.Domain.Context.ModerationContext.ValueObjects.Ids;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Entities;

namespace OIO.Domain.Context.ModerationContext.Aggregates;

public sealed class Report : BaseEntity<ReportId>, ICreatedAtEntity
{
    public UserId ReporterId { get; private set; }
    public string EntityType { get; private set; }
    public Guid EntityId { get; private set; }
    public string ReasonCode { get; private set; }
    public string? Description { get; private set; }
    public ReportStatus Status { get; private set; }
    public UserId? AssignedTo { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private Report() { }
}