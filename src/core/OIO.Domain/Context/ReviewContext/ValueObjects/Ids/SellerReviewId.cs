using OIO.Domain.SeedWork.Entities;
using Vogen;

namespace OIO.Domain.Context.ReviewContext.ValueObjects.Ids;

[ValueObject<Guid>] 
public readonly partial struct SellerReviewId : IEntityId;