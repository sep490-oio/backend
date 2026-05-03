using OIO.Domain.SeedWork.Entities;
using Vogen;

namespace OIO.Domain.Context.AssistantContext.ValueObjects.Ids;

[ValueObject<Guid>]
public readonly partial struct AssistantMessageId : IEntityId;
