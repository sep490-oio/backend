using CSharpFunctionalExtensions;

namespace OIO.Domain.Context.UserContext.Enums;

public sealed class UserStatus : EnumValueObject<UserStatus>
{
    public static readonly UserStatus Active = new("active");
    public static readonly UserStatus Inactive = new("inactive");
    public static readonly UserStatus Locked = new("locked");
    public static readonly UserStatus Suspended = new("suspended");
    
    private UserStatus (string id) : base(id)
    {
    }
}