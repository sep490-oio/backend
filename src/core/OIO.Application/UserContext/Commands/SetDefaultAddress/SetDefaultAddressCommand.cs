using OIO.Application.Abstractions.Messaging;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.UserContext.Commands.SetDefaultAddress;

public sealed record SetDefaultAddressCommand(Guid AddressId) : ICommand, IHasValidate
{
    public ViolationsError Validate()
    {
        return SetDefaultAddressCommand.Check()
            .WithOwnerName("SetDefaultAddress")
            .Field(AddressId)
            .NotEmptyGuid();
    }
}