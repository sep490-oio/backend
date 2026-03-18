using OIO.Domain.Context.ModerationContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Entities;

namespace OIO.Domain.Context.ModerationContext.Aggregates;

public sealed class DisputeResponseTemplate : BaseEntity<DisputeResponseTemplateId>, ICreatedAtEntity
{
    public string Name { get; private set; }
    public string? Category { get; private set; }
    public string? Subject { get; private set; }
    public string Body { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private DisputeResponseTemplate() { }
}