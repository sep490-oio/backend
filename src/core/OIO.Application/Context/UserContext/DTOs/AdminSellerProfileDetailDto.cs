namespace OIO.Application.Context.UserContext.DTOs;

public sealed record AdminSellerProfileDetailDto(
    SellerProfileDto Profile,
    UserDto User
);
