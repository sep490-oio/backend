using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using OIO.Application.Context.AddressContext.DTOs;

namespace OIO.Application.Context.AddressContext;

/// <summary>
/// Reads the local GHN address JSON files.
/// Files are at {ContentRoot}/data/provinces.json, districts.json, wards.json.
/// </summary>
internal static class AddressDataReader
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static async Task<List<ProvinceDto>> ReadProvincesAsync(
        IWebHostEnvironment env, CancellationToken ct = default)
    {
        var path = DataPath(env, "provinces.json");
        if (!File.Exists(path)) return [];
        await using var stream = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<List<ProvinceRaw>>(stream, _jsonOptions, ct)
               is { } raw
               ? raw.Select(r => new ProvinceDto(r.ProvinceID, r.ProvinceName)).ToList()
               : [];
    }

    public static async Task<List<DistrictDto>> ReadDistrictsAsync(
        IWebHostEnvironment env, int? provinceId = null, CancellationToken ct = default)
    {
        var path = DataPath(env, "districts.json");
        if (!File.Exists(path)) return [];
        await using var stream = File.OpenRead(path);
        var raw = await JsonSerializer.DeserializeAsync<List<DistrictRaw>>(stream, _jsonOptions, ct);
        if (raw is null) return [];
        var filtered = provinceId.HasValue
            ? raw.Where(d => d.ProvinceID == provinceId.Value)
            : raw.AsEnumerable();
        return filtered.Select(d => new DistrictDto(d.DistrictID, d.DistrictName, d.ProvinceID)).ToList();
    }

    public static async Task<List<WardDto>> ReadWardsAsync(
        IWebHostEnvironment env, int? districtId = null, CancellationToken ct = default)
    {
        var path = DataPath(env, "wards.json");
        if (!File.Exists(path)) return [];
        await using var stream = File.OpenRead(path);
        var raw = await JsonSerializer.DeserializeAsync<List<WardRaw>>(stream, _jsonOptions, ct);
        if (raw is null) return [];
        var filtered = districtId.HasValue
            ? raw.Where(w => w.DistrictID == districtId.Value)
            : raw.AsEnumerable();
        return filtered.Select(w => new WardDto(w.WardCode, w.WardName, w.DistrictID)).ToList();
    }

    private static string DataPath(IWebHostEnvironment env, string fileName)
        => Path.Combine(env.ContentRootPath, "data", fileName);

    // ─── Raw JSON shapes ─────────────────────────────────────────────────────
    private sealed record ProvinceRaw(int ProvinceID, string ProvinceName);
    private sealed record DistrictRaw(int DistrictID, string DistrictName, int ProvinceID);
    private sealed record WardRaw(string WardCode, string WardName, int DistrictID);
}
