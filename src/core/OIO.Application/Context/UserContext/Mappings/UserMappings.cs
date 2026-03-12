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

    public static readonly SortMappingDefinition UserListItemDtoSortMapping = SortMappingBuilder<UserListItemDto, User>
        .Create()
        .Map(x => x.EmailConfirmed, u => u.EmailConfirmed)
        .Map(x => x.Status, u => u.Status.Id)
        .Map(x => x.LastName, u => u.Profile.Name.LastName)
        .Map(x => x.FirstName, u => u.Profile.Name.FirstName)
        .Map(x => x.Email, u => u.Email.Value)
        .Map(x => x.UserName, u => u.UserName.Value)
        .Map(x => x.Id, u => u.Id.Value)
        .Build();
    
}