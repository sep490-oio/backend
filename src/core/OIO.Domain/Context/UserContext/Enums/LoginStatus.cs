using CSharpFunctionalExtensions;

namespace OIO.Domain.Context.UserContext.Enums;

public sealed class LoginStatus : EnumValueObject<LoginStatus>
{
    public static readonly LoginStatus Success =  new("success");
    public static readonly LoginStatus Failed =  new("failed");
    
    public LoginStatus(string id) : base(id)
    {
    }
}