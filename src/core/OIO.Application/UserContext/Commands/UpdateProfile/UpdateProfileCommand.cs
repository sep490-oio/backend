using OIO.Application.Abstractions.Messaging;
using OIO.Application.UserContext.DTOs;
using OIO.Domain.Constants;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;

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
                .MinLength(Constraints.FirstName.MinLength)
                .MinLength(Constraints.FirstName.MaxLength)
            )
            .Field(LastName)
            .WhenHasValue(x => x
                .NotWhiteSpace()
                .MinLength(Constraints.LastName.MinLength)
                .MaxLength(Constraints.LastName.MaxLength))
            .Field(DisplayName)
            .WhenHasValue(x => x
                .NotWhiteSpace()
                .MinLength(Constraints.DisplayName.MinLength)
                .MaxLength(Constraints.DisplayName.MaxLength))
            .Field(AvatarUrl)
            .WhenHasValue(x => x
                .NotWhiteSpace()
                .MinLength(Constraints.AvatarUrl.MinLength)
                .MaxLength(Constraints.AvatarUrl.MaxLength))
            .Field(DateOfBirth)
            .WhenHasValue(x => x.NotDefault())
            .Field(Gender)
            .WhenHasValue(x => x
                .NotWhiteSpace()
                .InSet(Domain.Context.UserContext.Enums.Gender.All.Select(g => g.Id))
            );
    }
}