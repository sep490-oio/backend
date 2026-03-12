using CSharpFunctionalExtensions;
using OIO.Domain.Context.Shared.ValueObjects;

namespace OIO.Domain.Context.OrderContext.ValueObjects;

public sealed class OrderPricing : ValueObject
{
    public Money ItemPrice { get; }
    public decimal ShippingFee { get; }
    public decimal PlatformFee { get; }
    public decimal TaxAmount { get; }
    public Money TotalAmount { get; }

    public OrderPricing()
    {
        
    }
    private OrderPricing(
        Money itemPrice,
        decimal shippingFee,
        decimal platformFee,
        decimal taxAmount,
        Money totalAmount)
    {
        ItemPrice = itemPrice;
        ShippingFee = shippingFee;
        PlatformFee = platformFee;
        TaxAmount = taxAmount;
        TotalAmount = totalAmount;
    }

    public static OrderPricing Create(
        Money itemPrice, 
        decimal shippingFee,
        decimal platformFee,
        decimal taxAmount,
        Money totalAmount)
        => new(itemPrice, shippingFee, platformFee, taxAmount, totalAmount);

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return ItemPrice.Amount;
        yield return ShippingFee;
        yield return PlatformFee;
        yield return TaxAmount;
        yield return TotalAmount.Amount;
    }
}