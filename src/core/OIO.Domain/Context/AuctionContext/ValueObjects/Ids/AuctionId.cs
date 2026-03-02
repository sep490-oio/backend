using OIO.Domain.SeedWork.Entities;
using Vogen;

namespace OIO.Domain.Context.AuctionContext.ValueObjects.Ids;

[ValueObject<Guid>]
public readonly partial struct AuctionId : IEntityId;

[ValueObject<Guid>]
public readonly partial struct ItemId : IEntityId;

[ValueObject<Guid>]
public readonly partial struct BidId : IEntityId;

[ValueObject<Guid>]
public readonly partial struct CategoryId : IEntityId;

[ValueObject<Guid>]
public readonly partial struct AuctionAutoBidId : IEntityId;

[ValueObject<Guid>]
public readonly partial struct AuctionDepositId : IEntityId;

[ValueObject<Guid>]
public readonly partial struct AuctionWatcherId : IEntityId;

[ValueObject<Guid>]
public readonly partial struct AuctionPriceHistoryId : IEntityId;

[ValueObject<Guid>]
public readonly partial struct ItemImageId : IEntityId;

[ValueObject<Guid>]
public readonly partial struct ItemQuestionId : IEntityId;