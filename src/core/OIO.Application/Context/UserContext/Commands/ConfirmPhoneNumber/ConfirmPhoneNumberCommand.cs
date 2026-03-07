using OIO.Application.Abstractions.Messaging;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.UserContext.Commands.ConfirmPhoneNumber;

public sealed record ConfirmPhoneNumberCommand(
    string VerificationCode) : ICommand, IHasValidate
{
    public ViolationsError Validate()
    {
        return ConfirmPhoneNumberCommand.Check()
            .WithOwnerName("ConfirmPhoneNumber")
            .Field(VerificationCode)
            .NotWhiteSpace();
    }
}