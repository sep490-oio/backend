using CSharpFunctionalExtensions;
using OIO.Domain.Context.Shared.Enums;
using OIO.Domain.Context.Shared.ValueObjects;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Domain.Context.AuctionContext.ValueObjects;

/// <summary>
/// Maps to: max_amount + current_amount + increment_amount + reserved_amount
/// Currency inherited from parent Auction — stored once, not per field.
///
/// Budget lifecycle:
///   Create(max=5M, increment=100K) → current=0, reserved=0
///   → PlaceBid → current ↑, reserved ↑
///   → Exhausted when remaining &lt; increment
/// </summary>
public sealed class AutoBidBudget : ValueObject
{
    // ── Private storage (EF Core maps these) ──
    public decimal MaxAmount { get; private init; }
    public decimal CurrentAmount { get; private init; }
    public decimal? IncrementAmount { get; private init; }
    public decimal ReservedAmount { get; private init; }
    public Currency Currency { get; private init; }

    // ── Public Money API ──
    public Money MaxPrice => Money.Of(MaxAmount, Currency);
    public Money CurrentPrice => Money.Of(CurrentAmount, Currency);
    public Money? Increment => IncrementAmount.HasValue
        ? Money.Of(IncrementAmount.Value, Currency)
        : null;
    public Money ReservedPrice => Money.Of(ReservedAmount, Currency);

    // ── Computed ──
    public Money Remaining => Money.Of(MaxAmount - CurrentAmount - ReservedAmount, Currency);
    public Money UsedTotal => Money.Of(CurrentAmount + ReservedAmount, Currency);
    public bool IsExhausted => CurrentAmount >= MaxAmount;
    public decimal UsagePercentage => MaxAmount > 0
        ? (CurrentAmount + ReservedAmount) / MaxAmount * 100
        : 0;

    private AutoBidBudget() { }

    private AutoBidBudget(
        decimal maxAmount,
        decimal currentAmount,
        decimal? incrementAmount,
        decimal reservedAmount,
        Currency currency)
    {
        MaxAmount = maxAmount;
        CurrentAmount = currentAmount;
        IncrementAmount = incrementAmount;
        ReservedAmount = reservedAmount;
        Currency = currency;
    }

    public static Result<AutoBidBudget, Error> Create(
        decimal maxAmount,
        Currency currency,
        decimal? incrementAmount = null)
    {
        var check = AutoBidBudget.Check(isInvariant: true)
            .Field(maxAmount, x => x.MaxAmount)
            .Positive()
            .Field(incrementAmount, x => x.IncrementAmount)
            .WhenHasValue(x => x.Positive()
                .LessThanOrEqual(maxAmount))
            .ToUnitResult();

        if (check.IsFailure)
        {
            return check.Error;
        }

        return new AutoBidBudget(
            maxAmount: maxAmount,
            currentAmount: 0,
            incrementAmount,
            reservedAmount: 0,
            currency: currency);
    }

    // ═════════════════════════════════════════════════════════════════
    // State transitions
    // ═════════════════════════════════════════════════════════════════

    /// <summary>Record a bid was placed at this amount</summary>
    public Result<AutoBidBudget, Error> WithBidPlaced(Money bidAmount)
    {
        var check = AutoBidBudget.Check(isInvariant: true)
            .Field(bidAmount.Currency.Id, x => x.Currency)
            .EqualTo(Currency.Id, error: Money.Errors.CurrencyMismatch(Currency.Id, bidAmount.Currency.Id))
            .Field(bidAmount, x => x.CurrentPrice)
            .LessThanOrEqual(MaxPrice)
            .ToUnitResult();

        if (check.IsFailure)
        {
            return check.Error;
        }

        return new AutoBidBudget(
            maxAmount: MaxAmount,
            currentAmount: bidAmount.Amount,
            incrementAmount: IncrementAmount,
            reservedAmount: ReservedAmount,
            currency: Currency);
    }

    /// <summary>Reserve amount for next potential bid. Currently unused — kept for future concurrent bidding support.</summary>
    public Result<AutoBidBudget, Error> WithReservation(Money amount)
    {
        var check = AutoBidBudget.Check(isInvariant: true)
            .Field(amount.Currency.Id, x => x.Currency)
            .EqualTo(Currency.Id, error: Money.Errors.CurrencyMismatch(Currency.Id, amount.Currency.Id))
            .Field(amount, x => x.ReservedAmount)
            .LessThanOrEqual(Remaining)
            .ToUnitResult();
        
        if (check.IsFailure)
        {
            return check.Error;
        }

        var newReserved = ReservedAmount + amount.Amount;

        return new AutoBidBudget(
            maxAmount: MaxAmount, 
            currentAmount: CurrentAmount, 
            incrementAmount: IncrementAmount,
            reservedAmount: newReserved,
            currency: Currency);
    }

 
    public AutoBidBudget WithReservationReleased(Money amount)
    {
        var newReserved = Math.Max(0, ReservedAmount - amount.Amount);
        
        return new AutoBidBudget(
            maxAmount: MaxAmount, 
            currentAmount: CurrentAmount,
            incrementAmount: IncrementAmount,
            reservedAmount: newReserved,
            currency: Currency);
    }

    /// <summary>Update max amount (user changes budget)</summary>
    public Result<AutoBidBudget, Error> WithMaxAmountUpdated(Money newMax)
    {
        var check = AutoBidBudget.Check(isInvariant: true)
            .Field(newMax.Currency.Id, x => x.Currency)
            .EqualTo(Currency.Id, error: Money.Errors.CurrencyMismatch(Currency.Id, newMax.Currency.Id))
            .Field(newMax, x => x.MaxAmount)
            .GreaterThanOrEqual(CurrentPrice)
            .ToUnitResult();
        
        if (check.IsFailure)
        {
            return check.Error;
        }

        return new AutoBidBudget(
            maxAmount: newMax.Amount,
            currentAmount: CurrentAmount,
            incrementAmount: IncrementAmount,
            reservedAmount: ReservedAmount,
            currency: Currency);
    }

    public Result<AutoBidBudget, Error> WithConfiguration(Money newMax, Money? newIncrement)
    {
        var check = AutoBidBudget.Check(isInvariant: true)
            .Field(newMax.Currency.Id, x => x.Currency)
            .EqualTo(Currency.Id, error: Money.Errors.CurrencyMismatch(Currency.Id, newMax.Currency.Id))
            .Field(newMax, x => x.MaxAmount)
            .GreaterThanOrEqual(CurrentPrice)
            .Field(newIncrement, x => x.Increment)
            .WhenHasValue(x => x
                .GreaterThan(Money.Zero(newMax.Currency))
                .LessThanOrEqual(newMax))
            .ToUnitResult();

        if (check.IsFailure)
        {
            return check.Error;
        }

        if (newIncrement is not null && newIncrement.Currency != Currency)
        {
            return Money.Errors.CurrencyMismatch(Currency.Id, newIncrement.Currency.Id);
        }

        return new AutoBidBudget(
            maxAmount: newMax.Amount,
            currentAmount: CurrentAmount,
            incrementAmount: newIncrement?.Amount,
            reservedAmount: ReservedAmount,
            currency: Currency);
    }

    // ═════════════════════════════════════════════════════════════════

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return MaxAmount;
        yield return CurrentAmount;
        yield return IncrementAmount ?? -1m;
        yield return ReservedAmount;
        yield return Currency.Id;
    }
}
