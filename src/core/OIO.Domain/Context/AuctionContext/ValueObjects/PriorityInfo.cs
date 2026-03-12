using CSharpFunctionalExtensions;

namespace OIO.Domain.Context.AuctionContext.ValueObjects;

public sealed class PriorityInfo : ValueObject
{
    public decimal Score { get; }
    public string Reason { get; }  // jsonb stored as string

    private PriorityInfo() {}
    private PriorityInfo(decimal score, string reason)
    {
        Score = score;
        Reason = reason;
    }

    public static PriorityInfo Create(decimal score, string reason = "{}")
        => new(score, reason);

    public static PriorityInfo Default => new(0, "{}");

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Score;
        yield return Reason;
    }
}