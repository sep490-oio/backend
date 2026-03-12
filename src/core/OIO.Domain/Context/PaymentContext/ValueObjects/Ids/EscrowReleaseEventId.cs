using OIO.Domain.SeedWork.Entities;
using Vogen;

namespace OIO.Domain.Context.PaymentContext.ValueObjects.Ids;

[ValueObject<Guid>] 
public readonly partial struct EscrowReleaseEventId : IEntityId;