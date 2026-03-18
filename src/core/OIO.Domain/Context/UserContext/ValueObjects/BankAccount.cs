using CSharpFunctionalExtensions;

namespace OIO.Domain.Context.UserContext.ValueObjects;

public sealed class BankAccount : ValueObject
{
    public string? BankName { get; }
    public string? AccountNumber { get; }
    public string? AccountHolder { get; }

    private BankAccount(string? bankName, string? accountNumber, string? accountHolder)
    {
        BankName = bankName;
        AccountNumber = accountNumber;
        AccountHolder = accountHolder;
    }

    public static BankAccount Create(
        string? bankName,
        string? accountNumber,
        string? accountHolder)
        => new(bankName?.Trim(), accountNumber?.Trim(), accountHolder?.Trim());

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return BankName ?? string.Empty;
        yield return AccountNumber ?? string.Empty;
        yield return AccountHolder ?? string.Empty;
    }
}