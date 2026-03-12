using CSharpFunctionalExtensions;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Domain.Context.OrderContext.ValueObjects;

public sealed class OrderNumber : ValueObject
{
    public const int MaxLength = 50;
    public string Value { get; }

    public OrderNumber() { }

    private OrderNumber(string value) => Value = value;

    public static Result<OrderNumber, Error> Create(string value)
    {
        var check = OrderNumber.Check(isInvariant: true)
            .Field(value)
            .NotWhiteSpace()
            .MaxLength(MaxLength)
            .ToUnitResult();
        
        if(check.IsFailure)
            return check.Error;
        
        return new OrderNumber(value.Trim());
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }
}