namespace OIO.Domain.Context.AuctionContext.Policies;

/// <summary>
/// Domain policy that computes the required deposit amount for auction participation.
/// Formula: max(startingPrice × 10%, 10.000₫ floor).
/// </summary>
public static class DepositPolicy
{
    /// <summary>Deposit is 10% of starting price.</summary>
    public const decimal DepositPercentage = 0.10m;

    /// <summary>Minimum deposit amount regardless of starting price (10.000 VND).</summary>
    public const decimal MinimumDepositAmount = 10_000m;

    /// <summary>
    /// Computes the required deposit for a given starting price.
    /// Returns max(startingPrice × <see cref="DepositPercentage"/>, <see cref="MinimumDepositAmount"/>).
    /// </summary>
    public static decimal ComputeRequiredDeposit(decimal startingPrice)
        => Math.Max(startingPrice * DepositPercentage, MinimumDepositAmount);
}
