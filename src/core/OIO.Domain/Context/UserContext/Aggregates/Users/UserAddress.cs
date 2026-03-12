using CSharpFunctionalExtensions;
using OIO.Domain.Context.UserContext.Enums;
using OIO.Domain.Context.UserContext.ValueObjects;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Entities;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Domain.Context.UserContext.Aggregates.Users;

public sealed class UserAddress : BaseEntity<UserAddressId>, IAuditableEntity
{
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    private UserAddress() {}
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    
    public UserId UserId { get; private set; }

    public AddressType Type { get; private set; }

    public RecipientInfo Recipient { get; private set; }

    public Address Address { get; private set; }

    public bool IsDefault { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public DateTime? ModifiedAt { get; private set; }
    
    internal UserAddress(
        UserId userId,
        AddressType type,
        RecipientInfo recipient,
        Address address,
        DateTime now,
        bool isDefault = false)
    {
        Id = UserAddressId.From(Guid.CreateVersion7());
        UserId = userId;
        Type = type;
        Recipient = recipient;
        Address = address;
        IsDefault = isDefault;
        CreatedAt = now;
    }

    internal UnitResult<Error> Update(
        AddressType? type,
        RecipientInfo? recipient,
        Address? address,
        DateTime now)
    {
        Type = type ?? Type;
        Recipient = recipient ?? Recipient;
        Address = address ?? Address;

        ModifiedAt = now;
        return UnitResult.Success<Error>();
    }

    internal void SetAsDefault() => IsDefault = true;
    internal void UnsetDefault() => IsDefault = false;
}