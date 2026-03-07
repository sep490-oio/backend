namespace OIO.Application.Context.UserContext.DTOs;

public sealed record UserAddressDto(
    Guid Id,
    string Type,
    string RecipientName,
    string PhoneNumber,
    string Street,
    string Ward,
    string District,
    string City,
    string? PostalCode,
    bool IsDefault);