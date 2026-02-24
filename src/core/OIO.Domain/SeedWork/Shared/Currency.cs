namespace OIO.Domain.SeedWork.Shared;

public record Currency
{
    public static readonly Currency VND = new("VND");

    private Currency(string code) => Code = code;

    public string Code { get; init; }

    public static Currency FromCode(string code)
    {
        return All.FirstOrDefault(c => c.Code == code) ??
               throw new ApplicationException("The currency code is invalid");
    }

    public static readonly IReadOnlyCollection<Currency> All = [VND];
}