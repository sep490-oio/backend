using CSharpFunctionalExtensions;

namespace OIO.Domain.Context.UserContext.Enums;

public sealed class IdType : EnumValueObject<IdType>
{
    public static readonly IdType Cccd = new("cccd");
    public static readonly IdType Cmnd = new("cmnd");
    public static readonly IdType Passport = new("passport");
    private IdType(string id) : base(id) { }
}