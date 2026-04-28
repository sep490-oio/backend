using CSharpFunctionalExtensions;
using OIO.Domain.AppDefinitions;
using OIO.Domain.Context.UserContext.Enums;
using OIO.Domain.Context.UserContext.Errors;
using OIO.Domain.Context.UserContext.ValueObjects;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Entities;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Domain.Context.UserContext.Aggregates.Users;

public sealed class UserProfile : BaseEntity<UserId>, IAuditableEntity
{
    private UserProfile() {}
    
    public PersonName Name { get; private set; }

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

    internal UnitResult<Error> Update(
        DateTime now,
        PersonName? name = null,
        AvatarUrl? avatarUrl = null,
        DateOnly? dateOfBirth = null,
        Gender? gender = null)
    {
        if (dateOfBirth.HasValue)
        {
            if (dateOfBirth.Value > DateOnly.FromDateTime(now))
            {
                return UserErrors.User.DateOfBirthInFuture;
            }

            if (dateOfBirth.Value.Year < App.Constraint.DateOfBirth.MinYear)
            {
                return UserErrors.User.DateOfBirthTooOld(App.Constraint.DateOfBirth.MinYear);
            }
        }

        Name = name ?? Name;
        AvatarUrl = avatarUrl ?? AvatarUrl;
        DateOfBirth = dateOfBirth ??  DateOfBirth;
        Gender = gender ?? Gender;

        ModifiedAt = now;

        return UnitResult.Success<Error>();
    }

    internal bool RefreshAvatarSnapshot(
        string oldPublicId,
        AvatarUrl avatarUrl,
        DateTime now)
    {
        if (AvatarUrl is null ||
            string.IsNullOrWhiteSpace(oldPublicId) ||
            !AvatarUrl.Value.Contains(oldPublicId, StringComparison.Ordinal))
        {
            return false;
        }

        AvatarUrl = avatarUrl;
        ModifiedAt = now;

        return true;
    }
}
