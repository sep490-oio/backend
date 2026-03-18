using OIO.Domain.SeedWork.Entities;
using Vogen;

namespace OIO.Domain.Context.ModerationContext.ValueObjects.Ids;

[ValueObject<Guid>]
public readonly partial struct MonitoringAlertId : IEntityId;