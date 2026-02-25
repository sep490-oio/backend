using OIO.Application.Abstractions.Messaging;
using OIO.Application.UserContext.DTOs;
using OIO.Domain.Constants;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.UserContext.Commands.RegisterUser;

public sealed record RegisterUserCommand(
    string UserName,
    string Email,
    string Password,
    string? FirstName = null,
    string? LastName = null) : ICommand<UserDto>, IHasValidate
{
    public ViolationsError Validate()
    {
        return RegisterUserCommand
            .Check()
            .WithOwnerName("RegisterUser")
            .Field(Email)
            .NotWhiteSpace()
            .MaxLength(Constraints.UserEmail.MaxLength)
            .Matches(Constraints.UserEmail.Regex)
            .Field(UserName)
            .NotWhiteSpace()
            .MinLength(Constraints.UserName.MinLength)
            .MaxLength(Constraints.UserName.MaxLength)
            .Matches(Constraints.UserName.Regex)
            .Field(Password)
            .NotWhiteSpace()
            .MinLength(Constraints.Password.MinLength)
            .MaxLength(Constraints.Password.MaxLength)
            .Format(
                message: Constraints.Password.FormatMessage,
                validators: Constraints.Password.Validator)
            .Field(FirstName)
            .When(FirstName is not null, x => 
                x.NotNullOrWhiteSpace()
                .MaxLength(Constraints.UserName.MaxLength)
                .MinLength(Constraints.UserName.MinLength)!)
            .Field(LastName)
            .When(LastName is not null, x => 
                x.NotNullOrWhiteSpace()
                .MaxLength(Constraints.UserName.MaxLength)
                .MinLength(Constraints.UserName.MinLength)!);
    }
}