using CSharpFunctionalExtensions;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Domain.Context.CatalogContext.ValueObjects;

public sealed class ItemTitle : ValueObject
{
    public const int MaxLength = 255;
    public string Value { get; }
    
    private ItemTitle() { }

    private ItemTitle(string value) => Value = value;

    public static Result<ItemTitle, Error> Create(string value)
    {
        var result = ItemTitle
            .Check(isInvariant: false)
            .Field(value)
            .NotWhiteSpace()
            .MaxLength(MaxLength)
            .ToUnitResult();
        
        if (result.IsFailure)
            return result.Error;
            
        return new ItemTitle(value);
    }

    public override string ToString()
    {
        return Value;
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }
}