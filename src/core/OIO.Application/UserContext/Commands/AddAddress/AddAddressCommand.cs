using OIO.Application.Abstractions.Messaging;
using OIO.Application.UserContext.DTOs;
using OIO.Domain.Context.UserContext.Enums;
using OIO.Domain.Context.UserContext.ValueObjects;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;
using OIO.Domain.AppDefinitions;

namespace OIO.Application.UserContext.Commands.AddAddress;

public sealed record AddAddressCommand(
    string Type,
    string RecipientName,
    string Street,
    string Ward,
    string District,
    string City,
    string? PostalCode,
    string PhoneNumber,
    string CountryCode = PhoneNumber.DefaultRegion,
    bool IsDefault = false) : ICommand<UserAddressDto>, IHasValidate
{
    public ViolationsError Validate()
    {
        return AddAddressCommand.Check()
            .WithOwnerName("AddAddress")
            .Field(Street)
            .NotWhiteSpace()
            .MaxLength(App.Constraint.Address.StreetMaxLength)
            .Field(District)
            .NotWhiteSpace()
            .MaxLength(App.Constraint.Address.DistrictMaxLength)
            .Field(Ward)
            .NotWhiteSpace()
            .MaxLength(App.Constraint.Address.WardMaxLength)
            .Field(City)
            .NotWhiteSpace()
            .MaxLength(App.Constraint.Address.CityMaxLength)
            .Field(PostalCode)
            .WhenHasValue(x => x.MaxLength(App.Constraint.Address.PostalCodeMaxLenght))
            .Field(Type)
            .NotWhiteSpace()
            .InSet(AddressType.All.Select(x => x.Id))
            .Field(CountryCode)
            .NotWhiteSpace()
            .Field(PhoneNumber)
            .NotWhiteSpace()
            .Field(RecipientName)
            .NotWhiteSpace()
            .MaxLength(App.Constraint.UserAddress.RecipientNameMaxLength);
    }
}