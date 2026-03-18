using OIO.Domain.SeedWork.Entities;
using Vogen;

namespace OIO.Domain.Context.AuctionContext.ValueObjects.Ids;

[ValueObject<Guid>] 
public readonly partial struct AuctionEmergencyActionId : IEntityId;