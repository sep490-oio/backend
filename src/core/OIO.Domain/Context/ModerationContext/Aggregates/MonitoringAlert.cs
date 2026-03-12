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
    public DateTime CreatedAt { get; private set; }

    private MonitoringAlert() { }
}