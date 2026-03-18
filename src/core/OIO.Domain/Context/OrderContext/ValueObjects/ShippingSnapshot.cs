using CSharpFunctionalExtensions;

namespace OIO.Domain.Context.OrderContext.ValueObjects;

public sealed class ShippingSnapshot : ValueObject
{
    public string? RecipientName { get; }
    public string? Phone { get; }
    public string Address { get; }
    public string? Ward { get; }
    public string? District { get; }
    public string? City { get; }

    public ShippingSnapshot()
    {
        
    }
    
    private ShippingSnapshot(
        string? recipientName, string? phone,
        string address, string? ward, string? district, string? city)
    {
        RecipientName = recipientName;
        Phone = phone;
        Address = address;
        Ward = ward;
        District = district;
        City = city;
    }

    public static ShippingSnapshot Create(
        string? recipientName,
        string? phone,
        string address,
        string? ward,
        string? district,
        string? city)
        => new(recipientName, phone, address, ward, district, city);

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return RecipientName ?? string.Empty;
        yield return Phone ?? string.Empty;
        yield return Address;
        yield return Ward ?? string.Empty;
        yield return District ?? string.Empty;
        yield return City ?? string.Empty;
    }
}