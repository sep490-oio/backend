using System.Net;
using OIO.Domain.Context.ModerationContext.ValueObjects.Ids;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Entities;

namespace OIO.Domain.Context.ModerationContext.Aggregates;

public sealed class AuditLog : BaseEntity<AuditLogId>, ICreatedAtEntity
{
    public UserId? ActorUserId { get; private set; }
    public string? ActorRole { get; private set; }
    public string Action { get; private set; }
    public string EntityType { get; private set; }
    public Guid? EntityId { get; private set; }
    public string? OldData { get; private set; }   // jsonb
    public string? NewData { get; private set; }   // jsonb
    public IPAddress? IpAddress { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private AuditLog() { }
}