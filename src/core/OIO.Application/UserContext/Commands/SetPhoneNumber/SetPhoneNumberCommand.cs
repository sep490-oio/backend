using OIO.Application.Abstractions.Messaging;
using OIO.Domain.Context.UserContext.ValueObjects;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.UserContext.Commands.SetPhoneNumber;

public sealed record SetPhoneNumberCommand(
    string PhoneNumber,
    string? CountryCode = PhoneNumber.DefaultRegion) : ICommand, IHasValidate
{
    public ViolationsError Validate()
    {
        return SetPhoneNumberCommand
            .Check()
            .WithOwnerName("SetPhoneNumber")
            .Field(PhoneNumber)!
            .NotNullOrWhiteSpace()
            .Field(CountryCode)
            .When(CountryCode is not null, 
                x => x.NotNullOrWhiteSpace()!);
    }
}