namespace OIO.Application.Abstractions.Address;

public sealed class GhnSyncResult
{
    public int ProvinceCount { get; init; }
    public int DistrictCount { get; init; }
    public int WardCount     { get; init; }
    public DateTime SyncedAt { get; init; }
}

public interface IGhnAddressService
{
    Task<GhnSyncResult> SyncAllAsync(CancellationToken ct = default);
}
