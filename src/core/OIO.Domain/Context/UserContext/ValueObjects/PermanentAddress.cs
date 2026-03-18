using CSharpFunctionalExtensions;

namespace OIO.Domain.Context.UserContext.ValueObjects;

public sealed class PermanentAddress : ValueObject
{
    public string FullAddress { get; }
    public string Province { get; }
    public string District { get; }
    public string Ward { get; }

    public PermanentAddress()
    {
        
    }
    
    private PermanentAddress(string fullAddress, string province, string district, string ward)
    {
        FullAddress = fullAddress;
        Province = province;
        District = district;
        Ward = ward;
    }

    public static PermanentAddress Create(string fullAddress, string province, string district, string ward)
        => new(fullAddress.Trim(), province.Trim(), district.Trim(), ward.Trim());

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return FullAddress;
        yield return Province;
        yield return District;
        yield return Ward;
    }
}