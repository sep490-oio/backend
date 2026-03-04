using OIO.Domain.SeedWork.Entities;
using Vogen;

namespace OIO.Domain.Context.NotificationContext.ValueObjects.Ids;

[ValueObject<Guid>]
public readonly partial struct NotificationId : IEntityId;

[ValueObject<Guid>]
public readonly partial struct NotificationDeliveryId : IEntityId;

[ValueObject<Guid>]
public readonly partial struct UserNotificationPreferenceId : IEntityId;
