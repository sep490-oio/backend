using OIO.Application.Abstractions.Sorting;
using OIO.Application.Context.UserContext.DTOs;
using OIO.Domain.Context.UserContext.Aggregates.Users;

namespace OIO.Application.Context.UserContext.Mappings;

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
    
    public static readonly SortMappingDefinition<UserListItemDto, User> SortMapping = new()
    {
        Mappings =
        [
            new SortMapping(nameof(UserListItemDto.EmailConfirmed), nameof(User.EmailConfirmed)),
            new SortMapping(nameof(UserListItemDto.Status), nameof(User.Status)),
            new SortMapping(nameof(UserListItemDto.LastName), nameof(User.Profile.LastName)),
            new SortMapping(nameof(UserListItemDto.FirstName), nameof(User.Profile.FirstName)),
            new SortMapping(nameof(UserListItemDto.Email), nameof(User.Email)),
            new SortMapping(nameof(UserListItemDto.UserName), nameof(User.UserName)),
            new SortMapping(nameof(UserListItemDto.Id), nameof(User.Id)),
          ]
    };
    
}