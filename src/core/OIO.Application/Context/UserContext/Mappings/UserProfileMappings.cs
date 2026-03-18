using OIO.Application.Context.UserContext.DTOs;
using OIO.Domain.Context.UserContext.Aggregates.Users;

namespace OIO.Application.Context.UserContext.Mappings;

internal static class UserProfileMappings
{
    public static UserProfileDto ToDto(this UserProfile profile)
    {
        return new UserProfileDto(
            FirstName: profile.Name?.FirstName,
            LastName: profile.Name?.LastName,
            DisplayName: profile.Name?.DisplayName,
            FullName: profile.Name?.FullName,
            AvatarUrl: profile.AvatarUrl?.Value,
            DateOfBirth: profile.DateOfBirth,
            Gender: profile.Gender?.Id);
    }
    
}