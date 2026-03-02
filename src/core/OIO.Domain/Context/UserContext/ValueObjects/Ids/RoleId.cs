using OIO.Domain.SeedWork.Entities;
using Vogen;

namespace OIO.Domain.Context.UserContext.ValueObjects.Ids;

[ValueObject]
public readonly partial struct RoleId : IEntityId; 

[ValueObject]
public readonly partial struct PermissionId : IEntityId;

[ValueObject<Guid>]
public readonly partial struct UserId : IEntityId;


[ValueObject<Guid>]
public readonly partial struct UserAddressId : IEntityId;


[ValueObject<Guid>]
public readonly partial struct UserLoginHistoryId : IEntityId;
 

[ValueObject<Guid>]
public readonly partial struct UserSessionId : IEntityId;


[ValueObject<Guid>]
public readonly partial struct UserRefreshTokenId : IEntityId;



[ValueObject<Guid>]
public readonly partial struct SellerProfileId : IEntityId;


[ValueObject<Guid>]
public readonly partial struct SellerKycId : IEntityId;

[ValueObject<Guid>]
public readonly partial struct SellerKycDocumentId : IEntityId;


[ValueObject<Guid>]
public readonly partial struct SellerKycHistoryId : IEntityId;