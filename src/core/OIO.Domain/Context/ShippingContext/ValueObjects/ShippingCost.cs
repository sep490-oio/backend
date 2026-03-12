using CSharpFunctionalExtensions;

namespace OIO.Domain.Context.ShippingContext.ValueObjects;

public sealed class ShippingCost : ValueObject
{
    public decimal ShippingFee { get; }
    public decimal InsuranceValue { get; }
    public decimal CodAmount { get; }

    public ShippingCost()
    {
        
    }
    
    private ShippingCost(
        decimal shippingFee,
        decimal insuranceValue,
        decimal codAmount)
    {
        ShippingFee = shippingFee;
        InsuranceValue = insuranceValue;
        CodAmount = codAmount;
    }

    public static ShippingCost Create(
        decimal shippingFee,
        decimal insuranceValue = 0,
        decimal codAmount = 0)
        => new(shippingFee, insuranceValue, codAmount);

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return ShippingFee;
        yield return InsuranceValue;
        yield return CodAmount;
    }
}