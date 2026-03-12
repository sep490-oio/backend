using CSharpFunctionalExtensions;

namespace OIO.Domain.Context.ShippingContext.ValueObjects;

public sealed class PickAddress : ValueObject
{
    public string Name { get; }
    public string Phone { get; }
    public string Address { get; }
    public string Ward { get; }
    public string District { get; }
    public string Province { get; }
    public string? CarrierAddressData { get; }  // jsonb

    public PickAddress()
    {
        
    }
    
    private PickAddress(
        string name, 
        string phone,
        string address,
        string ward,
        string district,
        string province,
        string? carrierAddressData)
    {
        Name = name;
        Phone = phone;
        Address = address;
        Ward = ward;
        District = district;
        Province = province;
        CarrierAddressData = carrierAddressData;
    }

    public static PickAddress Create(
        string name, 
        string phone,
        string address,
        string ward, 
        string district,
        string province,
        string? carrierAddressData = null)
        => new(name, phone, address, ward, district, province, carrierAddressData);

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Name;
        yield return Phone;
        yield return Address;
        yield return Ward;
        yield return District;
        yield return Province;
    }
}