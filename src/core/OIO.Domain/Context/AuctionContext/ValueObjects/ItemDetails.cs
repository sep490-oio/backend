using CSharpFunctionalExtensions;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Domain.Context.AuctionContext.ValueObjects;

public sealed class ItemDetails : ValueObject
{
    public string Name { get; }
    public string Description { get; }
    private ItemDetails() { }

    private ItemDetails(string name, string description)
    {
        Name = name;
        Description = description;
    }

    public static Result<ItemDetails, Error> Create(string name, string description)
    {
        var result = ItemDetails.Check(isInvariant: true)
            .Field(name, x => x.Name)!
            .NotNullOrWhiteSpace()
            .LengthBetween(5, 200)
            .Field(description, x => x.Description)!
            .NotNullOrWhiteSpace()
            .MaxLength(2000)
            .ToResult();

        if (result.IsFailure) return result.Error;

        return new ItemDetails(name.Trim(), description.Trim());
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Name;
        yield return Description;
    }
}