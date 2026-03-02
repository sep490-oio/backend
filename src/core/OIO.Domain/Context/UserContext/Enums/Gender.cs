using CSharpFunctionalExtensions;

namespace OIO.Domain.Context.UserContext.Enums;

public sealed class Gender : EnumValueObject<Gender>
{
    public static readonly Gender Male = new("male");
    public static readonly Gender Female = new("female");
    public static readonly Gender Other = new("other");
    private Gender(string id) : base(id) {}
}