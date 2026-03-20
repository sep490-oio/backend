using OIO.Domain.Context.ModerationContext.Enums;
using OIO.Domain.Context.ModerationContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Entities;

namespace OIO.Domain.Context.ModerationContext.Aggregates;

public sealed class MonitoringAlert : BaseEntity<MonitoringAlertId>, ICreatedAtEntity
{
    public string EntityType { get; private set; }
    public Guid EntityId { get; private set; }
    public string AlertType { get; private set; }
    public AlertSeverity Severity { get; private set; }
    public string Payload { get; private set; }  // jsonb
    public AlertStatus Status { get; private set; }
    public string? Notes { get; private set; }
    public Guid? AcknowledgedBy { get; private set; }
    public DateTime? AcknowledgedAt { get; private set; }
    public Guid? ResolvedBy { get; private set; }
    public DateTime? ResolvedAt { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private MonitoringAlert() { }

    public static MonitoringAlert Create(
        string entityType,
        Guid entityId,
        string alertType,
        AlertSeverity severity,
        string payload,
        DateTime nowUtc)
    {
        return new MonitoringAlert
        {
            Id = MonitoringAlertId.From(Guid.CreateVersion7()),
            EntityType = entityType,
            EntityId = entityId,
            AlertType = alertType,
            Severity = severity,
            Payload = payload,
            Status = AlertStatus.Open,
            CreatedAt = nowUtc
        };
    }

    public void Acknowledge(Guid adminId, string? notes, DateTime nowUtc)
    {
        Status = AlertStatus.Acknowledged;
        AcknowledgedBy = adminId;
        AcknowledgedAt = nowUtc;
        Notes = notes;
    }

    public void Resolve(Guid adminId, string? notes, bool ignored, DateTime nowUtc)
    {
        Status = ignored ? AlertStatus.Ignored : AlertStatus.Resolved;
        ResolvedBy = adminId;
        ResolvedAt = nowUtc;
        Notes = notes;
    }
}
