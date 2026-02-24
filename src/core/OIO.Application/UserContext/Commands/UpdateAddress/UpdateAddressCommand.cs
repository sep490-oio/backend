using OIO.Application.Abstractions.Messaging;
using OIO.Application.UserContext.DTOs;
using OIO.Domain.Constants;
using OIO.Domain.Context.UserContext.Enums;
using OIO.Domain.Context.UserContext.Repositories;
using OIO.Domain.Context.UserContext.ValueObjects;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.UserContext.Commands.UpdateAddress;

public sealed record UpdateAddressCommand(
    Guid AddressId,
    string? Type,
    string? RecipientName,
    string? Street,
    string? Ward,
    string? District,
    string? City,
    string? PhoneNumber,
    string? CountryCode = PhoneNumber.DefaultRegion,
    string? PostalCode = null) : ICommand<UserAddressDto>, IHasValidate
{
    public ViolationsError Validate()
    {
        return UpdateAddressCommand.Check()
            .WithOwnerName("UpdateAddress")
            .Field(AddressId)
            .NotEmptyGuid()
            .Field(Street)
            .WhenHasValue(x => x
                .NotWhiteSpace()
                .MaxLength(Constraints.Address.StreetMaxLength))
            .Field(District)
            .WhenHasValue(x => x
                .NotWhiteSpace()
                .MaxLength(Constraints.Address.DistrictMaxLength))
            .Field(Ward)
            .WhenHasValue(x => x
                .NotWhiteSpace()
                .MaxLength(Constraints.Address.WardMaxLength))
            .Field(City)
            .WhenHasValue(x => x
                .NotWhiteSpace()
                .MaxLength(Constraints.Address.CityMaxLength))
            .Field(PostalCode)
            .MaxLengthIfNotNull(Constraints.Address.PostalCodeMaxLenght)
            .Field(Type)
            .WhenHasValue(x => x
                .NotWhiteSpace()
                .InSet(AddressType.All.Select(type => type.Id)))
            .Field(CountryCode)
            .WhenHasValue(x => x
                .NotWhiteSpace())
            .Field(PhoneNumber)
            .WhenHasValue(x => x.NotWhiteSpace())
            .Field(RecipientName)
            .WhenHasValue(x => x
                .NotWhiteSpace()
                .MaxLength(Constraints.UserAddress.RecipientNameMaxLength));
    }
}