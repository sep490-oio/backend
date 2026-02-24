using OIO.Domain.SeedWork.Entities;

namespace OIO.Domain.Context.UserContext.ValueObjects;

public record UserId : GuidIdType<UserId>;
public record UserAddressId : GuidIdType<UserAddressId>;
public record UserLoginHistoryId : GuidIdType<UserLoginHistoryId>;
public record UserRefreshTokenFamilyId : GuidIdType<UserRefreshTokenFamilyId>;
public record UserRefreshTokenId : GuidIdType<UserRefreshTokenId>;
public record SellerProfileId : GuidIdType<SellerProfileId>;
public record SellerKycId : GuidIdType<SellerKycId>;
public record SellerKycDocumentId : GuidIdType<SellerKycDocumentId>;
public record SellerKycHistoryId : GuidIdType<SellerKycHistoryId>;
public record RoleId : GuidIdType<RoleId>;
public record PermissionId : GuidIdType<PermissionId>;

