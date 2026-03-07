using OIO.Application.Abstractions.Messaging;
using OIO.Domain.AppDefinitions;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.UserContext.Commands.ChangePassword;

public sealed record ChangePasswordCommand(
    string CurrentPassword,
    string NewPassword) : ICommand, IHasValidate
{
    public ViolationsError Validate()
    {
        return ChangePasswordCommand.Check()
            .WithOwnerName("ChangePassword")
            .Field(CurrentPassword)!
            .NotNullOrWhiteSpace()
            .Field(NewPassword)!
            .NotNullOrWhiteSpace()
            .MaxLength(App.Constraint.Password.MaxLength)
            .MinLength(App.Constraint.Password.MinLength)
            .Format(
                message: App.Constraint.Password.FormatMessage,
                validators: App.Constraint.Password.Validator);
    }
}