using OIO.Application.UserContext.DTOs;
using OIO.Domain.Context.UserContext.Aggregates.Users;

namespace OIO.Application.UserContext.Mappings;

internal static class UserMappings
{
    public static UserDto ToDto(this User user)
    {
        return new UserDto(
            Id: user.Id.Value,
            UserName: user.UserName,
            Email: user.Email.Value,
            EmailConfirmed: user.EmailConfirmed,
            PhoneNumber: user.PhoneNumber?.Value,
            CountryCode: user.PhoneNumber?.CountryCode,
            PhoneNumberConfirmed: user.PhoneNumberConfirmed,
            TwoFactorEnabled: user.TwoFactorEnabled,
            TwoFactorProvider: user.TwoFactorProvider.Id,
            Status: user.Status.Id,
            CreatedAt: user.CreatedAt,
            Profile: user.Profile?.ToDto());
    }

    public static UserProfileDto ToDto(this UserProfile profile)
    {
        return new UserProfileDto(
            FirstName: profile.FirstName?.Value,
            LastName: profile.LastName?.Value,
            DisplayName: profile.DisplayName?.Value,
            FullName: profile.FullName,
            AvatarUrl: profile.AvatarUrl?.Value,
            DateOfBirth: profile.DateOfBirth,
            Gender: profile.Gender?.Id);
    }

    public static UserAddressDto ToDto(this UserAddress address)
    {
        return new UserAddressDto(
            Id: address.Id.Value,
            Type: address.Type.Id,
            RecipientName: address.RecipientName,
            PhoneNumber: address.PhoneNumber,
            Street: address.Address.Street,
            Ward: address.Address.Ward,
            District: address.Address.District,
            City: address.Address.City,
            PostalCode: address.Address.PostalCode,
            IsDefault: address.IsDefault);
    }

    public static UserSummaryDto ToSummaryDto(this User user)
    {
        return new UserSummaryDto(
            Id: user.Id.Value,
            UserName: user.UserName,
            Email: user.Email.Value,
            DisplayName: user.Profile?.DisplayName?.Value,
            AvatarUrl: user.Profile?.AvatarUrl?.Value,
            Status: user.Status.Id,
            CreatedAt: user.CreatedAt);
    }
}