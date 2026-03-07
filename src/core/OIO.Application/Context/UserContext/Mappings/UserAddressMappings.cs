using OIO.Application.Context.UserContext.DTOs;
using OIO.Domain.Context.UserContext.Aggregates.Users;

namespace OIO.Application.Context.UserContext.Mappings;

internal static class UserAddressMappings
{
    public static UserAddressDto ToDto(this UserAddress address)
    {
        return new UserAddressDto(
            Id: address.Id.Value,
            Type: address.Type.Id,
            RecipientName: address.RecipientName,
            PhoneNumber: address.PhoneNumber,
            Street: address.Address.Street,
            Ward: address.Address.Ward,
            District: address.Address.District,
            City: address.Address.City,
            PostalCode: address.Address.PostalCode,
            IsDefault: address.IsDefault);
    }
    
}