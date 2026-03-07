using CSharpFunctionalExtensions;

namespace OIO.Domain.Context.Shared.ValueObjects;

public sealed class Currency : EnumValueObject<Currency>
{
    
    public static readonly Currency Vnd = new("VND");

    private Currency(string code) : base(code)
    {
    }
    public string Symbol => Id switch
    {
        "VND" => "₫",
        _ => throw new ApplicationException("The currency code is invalid")
    };
}