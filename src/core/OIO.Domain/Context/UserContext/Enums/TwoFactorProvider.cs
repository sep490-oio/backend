using CSharpFunctionalExtensions;

namespace OIO.Domain.Context.UserContext.Enums;

public sealed class TwoFactorProvider : EnumValueObject<TwoFactorProvider>
{
    public static readonly TwoFactorProvider None = new("none");
    public static readonly TwoFactorProvider Sms = new("sms");
    public static readonly TwoFactorProvider Email = new("email");
    
    private TwoFactorProvider(string id) : base(id)
    {
    }
}