using OIO.Domain.SeedWork.Entities;
using Vogen;

namespace OIO.Domain.Context.OrderContext.ValueObjects.Ids;

[ValueObject<Guid>]
public readonly partial struct SellerDirectShipmentEvidenceId : IEntityId;
