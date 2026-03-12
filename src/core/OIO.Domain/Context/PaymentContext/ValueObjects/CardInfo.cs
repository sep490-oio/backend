using CSharpFunctionalExtensions;

namespace OIO.Domain.Context.PaymentContext.ValueObjects;

public sealed class CardInfo : ValueObject
{
    public string? LastFour { get; }
    public int? ExpiryMonth { get; }
    public int? ExpiryYear { get; }
    public string? HolderName { get; }

    public CardInfo()
    {
        
    }
    
    private CardInfo(
        string? lastFour,
        int? expiryMonth,
        int? expiryYear,
        string? holderName)
    {
        LastFour = lastFour;
        ExpiryMonth = expiryMonth;
        ExpiryYear = expiryYear;
        HolderName = holderName;
    }

    public static CardInfo Create(
        string? lastFour,
        int? expiryMonth,
        int? expiryYear,
        string? holderName)
        => new(lastFour, expiryMonth, expiryYear, holderName);

    public bool IsExpired(int currentMonth, int currentYear)
    {
        if (!ExpiryMonth.HasValue || !ExpiryYear.HasValue) return false;
        return ExpiryYear.Value < currentYear ||
               (ExpiryYear.Value == currentYear && ExpiryMonth.Value < currentMonth);
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return LastFour ?? string.Empty;
        yield return ExpiryMonth ?? 0;
        yield return ExpiryYear ?? 0;
        yield return HolderName ?? string.Empty;
    }
}