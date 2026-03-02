using CSharpFunctionalExtensions;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;
using PhoneNumbers;

namespace OIO.Domain.Context.UserContext.ValueObjects;

public sealed class PhoneNumber : ValueObject
{
    private static readonly PhoneNumberUtil PhoneUtil = PhoneNumberUtil.GetInstance();
    public const string DefaultRegion = "VN";
    
    public string Value { get; private set;}
    public string CountryCode { get; private set;} // Ví dụ: VN, US

    private PhoneNumber(){}
    
    private PhoneNumber(string value, string countryCode)
    {
        Value = value;
        CountryCode = countryCode;
    }
    
    public static Result<PhoneNumber, Error> Create(string value, string? defaultRegion = DefaultRegion)
    {
        var check = PhoneNumber.Check(isInvariant: true)
            .Field(value, nameof(Value))!
            .NotNullOrWhiteSpace();
        
        if (check.IsFailure)
        {
            return check.ToViolationsError();
        }

        try
        {
            var numberProto = PhoneUtil.Parse(value, defaultRegion);
            
            var isValid = PhoneUtil.IsValidNumber(numberProto);
            if (!isValid)
                return Error.Format(
                    property: value,
                    isInvariant: true,
                    message: $"{value} is not valid phone number for {defaultRegion} region.");

            var formattedValue = PhoneUtil.Format(numberProto, PhoneNumberFormat.E164);
            
            var regionCode = PhoneUtil.GetRegionCodeForNumber(numberProto);

            return new PhoneNumber(formattedValue, regionCode);
        }
        catch (NumberParseException ex)
        {
            return Error.Format(
                property: value,
                isInvariant: true,
                message: ex.Message);
        }
    }

    public override string ToString() => Value;
    public static implicit operator string(PhoneNumber phoneNumber) => phoneNumber.Value;
    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
        yield return CountryCode;
    }
}