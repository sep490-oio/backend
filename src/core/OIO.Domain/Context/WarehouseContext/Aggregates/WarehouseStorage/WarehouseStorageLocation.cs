using OIO.Domain.Context.WarehouseContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Entities;

namespace OIO.Domain.Context.WarehouseContext.Aggregates.WarehouseStorage;

/// <summary>
/// Physical shelf/bin inside the warehouse.
/// Label format: {zone}-{aisle}-{shelf}-{bin} e.g. "A-01-03-02"
/// </summary>
public sealed class WarehouseStorageLocation : AggregateRoot<WarehouseStorageLocationId>
{
    private WarehouseStorageLocation() { }

    private WarehouseStorageLocation(
        WarehouseStorageLocationId id,
        string zone,
        string aisle,
        string shelf,
        string bin,
        DateTime now)
    {
        Id         = id;
        Zone       = zone;
        Aisle      = aisle;
        Shelf      = shelf;
        Bin        = bin;
        Label      = $"{zone}-{aisle}-{shelf}-{bin}";
        IsOccupied = false;
        CreatedAt  = now;
    }

    public string Zone { get; private set; }
    public string Aisle { get; private set; }
    public string Shelf { get; private set; }
    public string Bin { get; private set; }

    /// <summary>Human-readable composite label e.g. "A-01-03-02".</summary>
    public string Label { get; private set; }

    public bool IsOccupied { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public static WarehouseStorageLocation Create(
        string zone,
        string aisle,
        string shelf,
        string bin,
        DateTime now)
        => new(
            WarehouseStorageLocationId.From(Guid.CreateVersion7()),
            zone.ToUpper().Trim(),
            aisle.Trim(),
            shelf.Trim(),
            bin.Trim(),
            now);

    public void MarkOccupied() => IsOccupied = true;
    public void MarkVacant()   => IsOccupied = false;
    
    public void Update(string zone, string aisle, string shelf, string bin)
    {
        Zone  = zone.Trim().ToUpperInvariant();
        Aisle = aisle.Trim();
        Shelf = shelf.Trim();
        Bin   = bin.Trim();
        Label = $"{Zone}-{Aisle}-{Shelf}-{Bin}";
    }
}