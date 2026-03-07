using CSharpFunctionalExtensions;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Domain.Context.AuctionContext.ValueObjects;

public sealed class CategoryPath : ValueObject
{
    public string Value { get; }

    private CategoryPath(string value)
    {
        Value = value;
    }

    public static Result<CategoryPath, Error> Create(string path)
    {
        var result = CategoryPath
            .Check(isInvariant: true)
            .Field(path)
            .NotWhiteSpace()
            .ToResult();

        if (result.IsFailure)
        {
            return result.Error;
        }
        
        if (!path.StartsWith('/'))
            path = $"/{path}";

        return new CategoryPath(path.ToLowerInvariant().TrimEnd('/'));
    }

    public static Result<CategoryPath, Error> FromParent(CategoryPath? parentPath, string slug)
    {
        var result = CategoryPath
            .Check(isInvariant: true)
            .Field(slug)
            .NotWhiteSpace()
            .ToResult();
        
        if (result.IsFailure)
        {
            return result.Error;
        }

        var path = parentPath is not null
            ? $"{parentPath.Value}/{slug}"
            : $"/{slug}";

        return new CategoryPath(path.ToLowerInvariant());
    }

    public int Depth => Value.Count(c => c == '/');
    
    public bool IsRoot => Depth == 1;

    public bool IsDescendantOf(CategoryPath ancestor)
        => Value.StartsWith(ancestor.Value + "/", StringComparison.OrdinalIgnoreCase);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}