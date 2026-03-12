using CSharpFunctionalExtensions;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Domain.Context.ShippingContext.ValueObjects;

public sealed class StorageLocationCode : ValueObject
{
    public string Zone { get; }
    public string Aisle { get; }
    public string Shelf { get; }
    public string Bin { get; }
    public string Label { get; }  // e.g. A-01-03-02

    public StorageLocationCode()
    {
        
    }
    
    private StorageLocationCode(
        string zone,
        string aisle,
        string shelf,
        string bin,
        string label)
    {
        Zone = zone;
        Aisle = aisle;
        Shelf = shelf;
        Bin = bin;
        Label = label;
    }

    public static Result<StorageLocationCode, Error> Create(
        string zone,
        string aisle,
        string shelf,
        string bin)
    {
        var check = StorageLocationCode.Check()
            .Field(zone)
            .NotWhiteSpace()
            .Field(aisle)
            .NotWhiteSpace()
            .Field(shelf)
            .NotWhiteSpace()
            .Field(bin)
            .NotWhiteSpace()
            .ToUnitResult();

        if (check.IsFailure)
            return check.Error;

        var label = $"{zone}-{aisle}-{shelf}-{bin}";
        
        return new StorageLocationCode(zone.Trim(), aisle.Trim(), shelf.Trim(), bin.Trim(), label);
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Zone;
        yield return Aisle;
        yield return Shelf;
        yield return Bin;
    }
}