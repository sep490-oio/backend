using CSharpFunctionalExtensions;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;
using Error = OIO.Domain.SeedWork.Errors.Error;

namespace OIO.Domain.Context.UserContext.ValueObjects;

public sealed class Address : ValueObject
{
    private Address(){}
    private Address(string street, string ward, string district, string city, string? postalCode)
    {
        Street = street;
        Ward = ward;
        District = district;
        City = city;
        PostalCode = postalCode;
    }

    public string Street { get; private set; }
    public string Ward { get; private set; }
    public string District { get; private set; }
    public string City { get; private set; }
    public string? PostalCode { get; private set; }
    
    public static Result<Address, Error> Create(
        string street, 
        string ward, 
        string district, 
        string city, 
        string? postalCode = null)
    {
        var result = Address.Check(isInvariant: true)
            .Field(street, x => x.Street)!
            .NotNullOrWhiteSpace()
            .MaxLength(AppDefinitions.App.Constraint.Address.StreetMaxLength)
            .Field(district, x => x.District)!
            .NotNullOrWhiteSpace()
            .MaxLength(AppDefinitions.App.Constraint.Address.DistrictMaxLength)
            .Field(ward, x => x.Ward)!
            .NotNullOrWhiteSpace()
            .MaxLength(AppDefinitions.App.Constraint.Address.WardMaxLength)
            .Field(city, x => x.City)!
            .NotNullOrWhiteSpace()
            .MaxLength(AppDefinitions.App.Constraint.Address.CityMaxLength)
            .Field(postalCode, x => x.PostalCode)
            .WhenHasValue(x => x.MaxLength(AppDefinitions.App.Constraint.Address.PostalCodeMaxLenght))
            .ToResult();

        if (result.IsFailure)
            return result.Error;
        
        return new Address(street.Trim(), ward.Trim(), district.Trim(), city.Trim(), postalCode?.Trim());
    }

    public string FullAddress => $"{Street}, {Ward}, {District}, {City}";
    
    public override string ToString() => FullAddress;
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Street;
        yield return Ward;
        yield return District;
        yield return City;
        yield return PostalCode;
    }
}