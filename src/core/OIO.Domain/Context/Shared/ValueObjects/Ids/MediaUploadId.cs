using OIO.Domain.SeedWork.Entities;
using Vogen;

namespace OIO.Domain.Context.Shared.ValueObjects.Ids;

[ValueObject<Guid>]
public readonly partial struct MediaUploadId : IEntityId;
