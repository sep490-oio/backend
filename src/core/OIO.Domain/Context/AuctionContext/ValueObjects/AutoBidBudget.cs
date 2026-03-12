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
    private decimal _maxAmount;
    private decimal _currentAmount;
    private decimal? _incrementAmount;
    private decimal _reservedAmount;
    private string _currency;

    // ── Public Money API ──
    public Money MaxAmount => Money.Of(_maxAmount, new Currency(_currency));
    public Money CurrentAmount => Money.Of(_currentAmount, new Currency(_currency));
    public Money? IncrementAmount => _incrementAmount.HasValue
        ? Money.Of(_incrementAmount.Value, new Currency(_currency))
        : null;
    public Money ReservedAmount => Money.Of(_reservedAmount, new Currency(_currency));
    public Currency Currency => new Currency(_currency);

    // ── Computed ──
    public Money Remaining => Money.Of(_maxAmount - _currentAmount - _reservedAmount, new Currency(_currency));
    public Money UsedTotal => Money.Of(_currentAmount + _reservedAmount, new Currency(_currency));
    public bool IsExhausted => _incrementAmount.HasValue
        ? Remaining < IncrementAmount!
        : Remaining.IsZero();
    public decimal UsagePercentage => _maxAmount > 0
        ? (_currentAmount + _reservedAmount) / _maxAmount * 100
        : 0;

    private AutoBidBudget() { }

    private AutoBidBudget(
        decimal maxAmount,
        decimal currentAmount,
        decimal? incrementAmount,
        decimal reservedAmount,
        string currency)
    {
        _maxAmount = maxAmount;
        _currentAmount = currentAmount;
        _incrementAmount = incrementAmount;
        _reservedAmount = reservedAmount;
        _currency = currency;
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
            currency: currency.Id);
    }

    // ═════════════════════════════════════════════════════════════════
    // State transitions
    // ═════════════════════════════════════════════════════════════════

    /// <summary>Record a bid was placed at this amount</summary>
    public Result<AutoBidBudget, Error> WithBidPlaced(Money bidAmount)
    {
        var check = AutoBidBudget.Check(isInvariant: true)
            .Field(bidAmount.Currency.Id, x => x.Currency)
            .EqualTo(_currency, error: Money.Errors.CurrencyMismatch(_currency, bidAmount.Currency.Id))
            .Field(bidAmount, x => x.CurrentAmount)
            .LessThanOrEqual(MaxAmount)
            .ToUnitResult();

        if (check.IsFailure)
        {
            return check.Error;
        }

        return new AutoBidBudget(
            maxAmount: _maxAmount,
            currentAmount: bidAmount.Amount,
            incrementAmount: _incrementAmount,
            reservedAmount: _reservedAmount,
            currency: _currency);
    }

    /// <summary>Reserve amount for next potential bid</summary>
    public Result<AutoBidBudget, Error> WithReservation(Money amount)
    {
        var check = AutoBidBudget.Check(isInvariant: true)
            .Field(amount.Currency.Id, x => x.Currency)
            .EqualTo(_currency, error: Money.Errors.CurrencyMismatch(_currency, amount.Currency.Id))
            .Field(amount, x => x.ReservedAmount)
            .LessThanOrEqual(Remaining)
            .ToUnitResult();
        
        if (check.IsFailure)
        {
            return check.Error;
        }

        var newReserved = _reservedAmount + amount.Amount;

        return new AutoBidBudget(
            maxAmount: _maxAmount, 
            currentAmount: _currentAmount, 
            incrementAmount: _incrementAmount,
            reservedAmount: newReserved,
            currency: _currency);
    }

 
    public AutoBidBudget WithReservationReleased(Money amount)
    {
        var newReserved = Math.Max(0, _reservedAmount - amount.Amount);
        
        return new AutoBidBudget(
            maxAmount: _maxAmount, 
            currentAmount: _currentAmount,
            incrementAmount: _incrementAmount,
            reservedAmount: newReserved,
            currency: _currency);
    }

    /// <summary>Update max amount (user changes budget)</summary>
    public Result<AutoBidBudget, Error> WithMaxAmountUpdated(Money newMax)
    {
        var check = AutoBidBudget.Check(isInvariant: true)
            .Field(newMax.Currency.Id, x => x.Currency)
            .EqualTo(_currency, error: Money.Errors.CurrencyMismatch(_currency, newMax.Currency.Id))
            .Field(newMax, x => x.MaxAmount)
            .GreaterThanOrEqual(CurrentAmount)
            .ToUnitResult();
        
        if (check.IsFailure)
        {
            return check.Error;
        }

        return new AutoBidBudget(
            maxAmount: newMax.Amount,
            currentAmount: _currentAmount,
            incrementAmount: _incrementAmount,
            reservedAmount: _reservedAmount,
            currency: _currency);
    }

    // ═════════════════════════════════════════════════════════════════

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return _maxAmount;
        yield return _currentAmount;
        yield return _incrementAmount ?? -1m;
        yield return _reservedAmount;
        yield return _currency;
    }
}