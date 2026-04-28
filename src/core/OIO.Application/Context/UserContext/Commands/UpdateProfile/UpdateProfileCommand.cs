using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.UserContext.DTOs;
using OIO.Domain.AppDefinitions;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.UserContext.Commands.UpdateProfile;

public sealed record UpdateProfileCommand(
    string? FirstName,
    string? LastName,
    string? DisplayName,
    Guid? AvatarMediaUploadId,
    DateOnly? DateOfBirth,
    string? Gender) : ICommand<UserProfileDto>, IHasValidate
{
    public ViolationsError Validate()
    {
        return UpdateProfileCommand.Check()
            .WithOwnerName("UpdateProfile")
            .Field(FirstName)
            .WhenHasValue(x => x
                .NotWhiteSpace()
                .MinLength(App.Constraint.FirstName.MinLength)
                .MaxLength(App.Constraint.FirstName.MaxLength)
            )
            .Field(LastName)
            .WhenHasValue(x => x
                .NotWhiteSpace()
                .MinLength(App.Constraint.LastName.MinLength)
                .MaxLength(App.Constraint.LastName.MaxLength))
            .Field(DisplayName)
            .WhenHasValue(x => x
                .NotWhiteSpace()
                .MinLength(App.Constraint.DisplayName.MinLength)
                .MaxLength(App.Constraint.DisplayName.MaxLength))
            .Field(AvatarMediaUploadId)
            .WhenHasValue(x => x.NotEmptyGuid())
            .Field(DateOfBirth)
            .WhenHasValue(x => x
                .NotDefault()
                .NotInFuture()
                .After(new DateOnly(App.Constraint.DateOfBirth.MinYear, 1, 1)))
            .Field(Gender)
            .WhenHasValue(x => x
                .NotWhiteSpace()
                .InSet(Domain.Context.UserContext.Enums.Gender.All.Select(g => g.Id))
            );
    }
}
