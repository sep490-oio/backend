using CSharpFunctionalExtensions;
using OIO.Domain.Constants;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;
using Error = OIO.Domain.SeedWork.Errors.Error;

namespace OIO.Domain.Context.UserContext.ValueObjects;

public sealed class Address : ValueObject
{
    public string Street { get; }
    public string Ward { get; }
    public string District { get; }
    public string City { get; }
    public string? PostalCode { get; }

    
    private Address(string street, string ward, string district, string city, string? postalCode)
    {
        Street = street;
        Ward = ward;
        District = district;
        City = city;
        PostalCode = postalCode;
    }

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
            .MaxLength(Constraints.Address.StreetMaxLength)
            .Field(district, x => x.District)!
            .NotNullOrWhiteSpace()
            .MaxLength(Constraints.Address.DistrictMaxLength)
            .Field(ward, x => x.Ward)!
            .NotNullOrWhiteSpace()
            .MaxLength(Constraints.Address.WardMaxLength)
            .Field(city, x => x.City)!
            .NotNullOrWhiteSpace()
            .MaxLength(Constraints.Address.CityMaxLength)
            .Field(postalCode, x => x.PostalCode)
            .MaxLengthIfNotNull(Constraints.Address.PostalCodeMaxLenght)
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