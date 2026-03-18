using CSharpFunctionalExtensions;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Domain.Context.CatalogContext.ValueObjects;

public sealed class Slug : ValueObject
{
    public string Value { get; }

    private Slug(){}
    
    private Slug(string value) => Value = value;

    public static Result<Slug, Error> Create(string value)
    {
        var result = Slug
            .Check(isInvariant: true)
            .Field(value)
            .NotWhiteSpace()
            .MaxLength(100)
            .ToResult();
        
        if (result.IsFailure)
            return result.Error;
            
        return new Slug(value);
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