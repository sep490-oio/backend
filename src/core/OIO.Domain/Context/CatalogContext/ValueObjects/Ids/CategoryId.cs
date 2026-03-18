using OIO.Domain.SeedWork.Entities;
using Vogen;

namespace OIO.Domain.Context.CatalogContext.ValueObjects.Ids;

[ValueObject<Guid>]
public readonly partial struct CategoryId : IEntityId;