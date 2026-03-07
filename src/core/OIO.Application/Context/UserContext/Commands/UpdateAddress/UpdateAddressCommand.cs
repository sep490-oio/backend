using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.UserContext.DTOs;
using OIO.Domain.AppDefinitions;
using OIO.Domain.Context.UserContext.Enums;
using OIO.Domain.Context.UserContext.ValueObjects;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.UserContext.Commands.UpdateAddress;

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
                .MaxLength(App.Constraint.Address.StreetMaxLength))
            .Field(District)
            .WhenHasValue(x => x
                .NotWhiteSpace()
                .MaxLength(App.Constraint.Address.DistrictMaxLength))
            .Field(Ward)
            .WhenHasValue(x => x
                .NotWhiteSpace()
                .MaxLength(App.Constraint.Address.WardMaxLength))
            .Field(City)
            .WhenHasValue(x => x
                .NotWhiteSpace()
                .MaxLength(App.Constraint.Address.CityMaxLength))
            .Field(PostalCode)
            .WhenHasValue(x => x.MaxLength(App.Constraint.Address.PostalCodeMaxLenght))
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
                .MaxLength(App.Constraint.UserAddress.RecipientNameMaxLength));
    }
}