using CSharpFunctionalExtensions;
using OIO.Domain.Context.Shared.ValueObjects;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Domain.Context.AuctionContext.Grains.GrainValueObjects;

[GenerateSerializer]
[Alias("OIO.Domain.Context.AuctionContext.Grains.GrainValueObjects.MoneyGrain")]
public struct MoneyGrain
{
    [Id(0)]
    public decimal Amount { get; set; }
    [Id(1)]
    public string Currency { get; set; }
    
    public static MoneyGrain From(Money money)
    {
        return new MoneyGrain
        {
            Amount = money.Amount,
            Currency = money.Currency.Id
        };
    }
    
    public static Result<Money, Error> ToMoney(MoneyGrain moneyGrain)
    {
        return Money.Create(moneyGrain.Amount,moneyGrain.Currency);
    }
}