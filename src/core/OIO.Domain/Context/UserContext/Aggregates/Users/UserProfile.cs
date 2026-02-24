using CSharpFunctionalExtensions;
using OIO.Domain.Context.UserContext.Enums;
using OIO.Domain.Context.UserContext.ValueObjects;
using OIO.Domain.SeedWork.Entities;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Domain.Context.UserContext.Aggregates.Users;

public sealed class UserProfile : SeedWork.Entities.Entity<UserId>, IAuditableEntity
{
    private UserProfile() {}
    
    public FirstName? FirstName { get; private set; }

    public LastName? LastName { get; private set; }

    public DisplayName? DisplayName { get; private set; }

    public AvatarUrl? AvatarUrl { get; private set; }

    public DateOnly? DateOfBirth { get; private set; }

    public Gender? Gender { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public DateTime? ModifiedAt { get; private set; }
    
    internal UserProfile(UserId userId, DateTime createdAt)
    {
        Id = userId;
        CreatedAt = createdAt;
    }

    public string? FullName =>
        (FirstName, LastName) switch
        {
            (not null, not null) => $"{FirstName} {LastName}",
            (not null, null) => FirstName,
            (null, not null) => LastName,
            _ => null
        };

    internal UnitResult<Error> Update(
        FirstName? firstName,
        LastName? lastName,
        DisplayName? displayName,
        AvatarUrl? avatarUrl,
        DateOnly? dateOfBirth,
        Gender? gender,
        DateTime now)
    {
        FirstName = firstName ?? FirstName;
        LastName = lastName ?? LastName;
        DisplayName = displayName ?? DisplayName;
        AvatarUrl = avatarUrl ?? AvatarUrl;
        DateOfBirth = dateOfBirth ??  DateOfBirth;
        Gender = gender ?? gender;

        ModifiedAt = now;

        return UnitResult.Success<Error>();
    }
}