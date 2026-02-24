using OIO.Application.Abstractions.Messaging;
using OIO.Domain.Constants;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.UserContext.Commands.ChangePassword;

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
            .MaxLength(Constraints.Password.MaxLength)
            .MinLength(Constraints.Password.MinLength)
            .Format(
                message: Constraints.Password.FormatMessage,
                validators: Constraints.Password.Validator);
    }
}