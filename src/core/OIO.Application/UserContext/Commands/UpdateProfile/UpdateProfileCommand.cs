using OIO.Application.Abstractions.Messaging;
using OIO.Application.UserContext.DTOs;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;
using OIO.Domain.AppDefinitions;

namespace OIO.Application.UserContext.Commands.UpdateProfile;

public sealed record UpdateProfileCommand(
    string? FirstName,
    string? LastName,
    string? DisplayName,
    string? AvatarUrl,
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
                .MinLength(App.Constraint.FirstName.MaxLength)
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
            .Field(AvatarUrl)
            .WhenHasValue(x => x
                .NotWhiteSpace()
                .MinLength(App.Constraint.AvatarUrl.MinLength)
                .MaxLength(App.Constraint.AvatarUrl.MaxLength))
            .Field(DateOfBirth)
            .WhenHasValue(x => x.NotDefault())
            .Field(Gender)
            .WhenHasValue(x => x
                .NotWhiteSpace()
                .InSet(Domain.Context.UserContext.Enums.Gender.All.Select(g => g.Id))
            );
    }
}