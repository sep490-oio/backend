using OIO.Application.Context.AuctionContext.DTOs;
using OIO.Domain.Context.AuctionContext.Grains.GrainValueObjects;
using OIO.Domain.Context.Shared.ValueObjects;

namespace OIO.Application.Context.AuctionContext.Mappings;

internal static class MoneyMappings
{
    public static MoneyDto ToDto(this Money money)
    {
        return new MoneyDto(money.Amount, money.Currency.Id, money.Currency.Symbol);
    }

    public static MoneyDto ToDto(this MoneyGrain money)
    {
        return new MoneyDto(money.Amount, money.Currency, Currency.GetSymbol(money.Currency));
    }
}