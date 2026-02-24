using OIO.Application.Abstractions.Messaging;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.UserContext.Commands.RemoveAddress;

public sealed record RemoveAddressCommand(Guid AddressId) : ICommand, IHasValidate
{
    public ViolationsError Validate()
    {
        return RemoveAddressCommand
            .Check()
            .WithOwnerName("RemoveAddress")
            .Field(AddressId)
            .NotEmptyGuid();
    }
}