using CSharpFunctionalExtensions;

namespace OIO.Domain.Context.UserContext.Enums;

public sealed class AddressType : EnumValueObject<AddressType>
{
    public static readonly AddressType Home =  new("home");
    public static readonly AddressType Work = new("work");
    public static readonly AddressType Other = new("other");
    
    public AddressType(string id) : base(id)
    {
    }
}