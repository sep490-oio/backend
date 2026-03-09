using OIO.Domain.SeedWork.Entities;
using Vogen;

namespace OIO.Domain.Context.UserContext.ValueObjects.Ids;

[ValueObject<Guid>]
public readonly partial struct SellerKycHistoryId : IEntityId;