using CSharpFunctionalExtensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Domain.Context.OrderContext.ValueObjects;

public sealed class ShippingSnapshot : ValueObject
{
    public string? RecipientName { get; }
    public string? Phone { get; }
    /// <summary>
    /// Legacy free-text address line. Kept for backward compatibility with
    /// orders created before the structured shipping snapshot feature. New
    /// orders should populate <see cref="Street"/> + <see cref="Ward"/>+…
    /// and use this as a composed display string.
    /// </summary>
    public string Address { get; }
    public string? Street { get; }
    public string? Ward { get; }
    public string? District { get; }
    public string? City { get; }
    public string? PostalCode { get; }

    public ShippingSnapshot()
    {
        Address = string.Empty;
    }

    private ShippingSnapshot(
        string? recipientName,
        string? phone,
        string address,
        string? street,
        string? ward,
        string? district,
        string? city,
        string? postalCode)
    {
        RecipientName = recipientName;
        Phone = phone;
        Address = address;
        Street = street;
        Ward = ward;
        District = district;
        City = city;
        PostalCode = postalCode;
    }

    /// <summary>
    /// Legacy factory: accepts a free-form address line plus optional administrative parts.
    /// Used by initial Order.Create() flows. Does NOT validate strictly.
    /// </summary>
    public static ShippingSnapshot Create(
        string? recipientName,
        string? phone,
        string address,
        string? ward,
        string? district,
        string? city)
        => new(recipientName, phone, address, street: null, ward, district, city, postalCode: null);

    /// <summary>
    /// Structured factory used by the "update shipping before payment" flow.
    /// Mirrors the address-book rules: recipient, phone, street, ward, district, city are required;
    /// postal code is optional. The legacy <see cref="Address"/> field is composed from the
    /// structured parts so existing displays keep working.
    /// </summary>
    public static Result<ShippingSnapshot, Error> CreateStructured(
        string recipientName,
        string phone,
        string street,
        string ward,
        string district,
        string city,
        string? postalCode)
    {
        if (string.IsNullOrWhiteSpace(recipientName))
            return Error.Validation("recipientName", "ShippingSnapshot.RecipientRequired", "Recipient name is required.");
        if (string.IsNullOrWhiteSpace(phone))
            return Error.Validation("phoneNumber", "ShippingSnapshot.PhoneRequired", "Phone number is required.");
        if (string.IsNullOrWhiteSpace(street))
            return Error.Validation("street", "ShippingSnapshot.StreetRequired", "Street is required.");
        if (string.IsNullOrWhiteSpace(ward))
            return Error.Validation("ward", "ShippingSnapshot.WardRequired", "Ward is required.");
        if (string.IsNullOrWhiteSpace(district))
            return Error.Validation("district", "ShippingSnapshot.DistrictRequired", "District is required.");
        if (string.IsNullOrWhiteSpace(city))
            return Error.Validation("city", "ShippingSnapshot.CityRequired", "City is required.");

        var trimmedStreet = street.Trim();
        var trimmedWard = ward.Trim();
        var trimmedDistrict = district.Trim();
        var trimmedCity = city.Trim();
        var composed = $"{trimmedStreet}, {trimmedWard}, {trimmedDistrict}, {trimmedCity}";

        return new ShippingSnapshot(
            recipientName: recipientName.Trim(),
            phone: phone.Trim(),
            address: composed,
            street: trimmedStreet,
            ward: trimmedWard,
            district: trimmedDistrict,
            city: trimmedCity,
            postalCode: string.IsNullOrWhiteSpace(postalCode) ? null : postalCode.Trim());
    }

    /// <summary>
    /// Returns true if the snapshot carries a real, fully-populated address
    /// (produced via <see cref="CreateStructured"/>). Legacy/placeholder
    /// snapshots lacking structured parts return false so the checkout flow
    /// can force the buyer to re-enter before paying.
    /// </summary>
    public bool IsStructured =>
        !string.IsNullOrWhiteSpace(Street) &&
        !string.IsNullOrWhiteSpace(Ward) &&
        !string.IsNullOrWhiteSpace(District) &&
        !string.IsNullOrWhiteSpace(City) &&
        !string.IsNullOrWhiteSpace(RecipientName) &&
        !string.IsNullOrWhiteSpace(Phone);

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return RecipientName ?? string.Empty;
        yield return Phone ?? string.Empty;
        yield return Address;
        yield return Street ?? string.Empty;
        yield return Ward ?? string.Empty;
        yield return District ?? string.Empty;
        yield return City ?? string.Empty;
        yield return PostalCode ?? string.Empty;
    }
}
