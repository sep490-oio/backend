using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Options;
using OIO.Application.Abstractions.Address;
using OIO.Infrastructure.Settings;

namespace OIO.Infrastructure.Shipping.Ghn;

// ─── Raw GHN response wrappers ───────────────────────────────────────────────

internal sealed class GhnProvinceListResponse
{
    [JsonPropertyName("code")]   public int Code { get; init; }
    [JsonPropertyName("data")]   public List<GhnProvinceItem> Data { get; init; } = [];
}

internal sealed class GhnProvinceItem
{
    [JsonPropertyName("ProvinceID")]   public int   ProvinceId   { get; init; }
    [JsonPropertyName("ProvinceName")] public string ProvinceName { get; init; } = string.Empty;
}

internal sealed class GhnDistrictListResponse
{
    [JsonPropertyName("code")]   public int Code { get; init; }
    [JsonPropertyName("data")]   public List<GhnDistrictItem>? Data { get; init; }
}

internal sealed class GhnDistrictItem
{
    [JsonPropertyName("DistrictID")]   public int    DistrictId   { get; init; }
    [JsonPropertyName("DistrictName")] public string DistrictName { get; init; } = string.Empty;
    [JsonPropertyName("ProvinceID")]   public int    ProvinceId   { get; init; }
}

internal sealed class GhnWardListResponse
{
    [JsonPropertyName("code")]   public int Code { get; init; }
    [JsonPropertyName("data")]   public List<GhnWardItem>? Data { get; init; }
}

internal sealed class GhnWardItem
{
    [JsonPropertyName("WardCode")]   public string WardCode   { get; init; } = string.Empty;
    [JsonPropertyName("WardName")]   public string WardName   { get; init; } = string.Empty;
    [JsonPropertyName("DistrictID")] public int    DistrictId { get; init; }
}

// ─── Service ─────────────────────────────────────────────────────────────────

internal sealed class GhnAddressService : IGhnAddressService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string             _token;
    private readonly string             _baseUrl;
    private readonly string             _dataDir;

    private static readonly JsonSerializerOptions _readOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static readonly JsonSerializerOptions _writeOptions = new()
    {
        WriteIndented = false
    };

    public GhnAddressService(
        IHttpClientFactory httpClientFactory,
        IOptions<GhnAddressOptions> options,
        IWebHostEnvironment env)
    {
        _httpClientFactory = httpClientFactory;
        _token             = options.Value.Token;
        _baseUrl           = options.Value.BaseUrl;
        _dataDir           = Path.Combine(env.ContentRootPath, "data");
    }

    public async Task<GhnSyncResult> SyncAllAsync(CancellationToken ct = default)
    {
        Directory.CreateDirectory(_dataDir);

        using var http = BuildClient();

        // ──── 1. Provinces ────────────────────────────────────────────────────
        var provinces = await FetchProvincesAsync(http, ct);

        // ──── 2. Districts (one call per province) ───────────────────────────
        var allDistricts = new List<GhnDistrictItem>();
        foreach (var province in provinces)
        {
            var districts = await FetchDistrictsAsync(http, province.ProvinceId, ct);
            allDistricts.AddRange(districts);
        }

        // ──── 3. Wards (one call per district) ───────────────────────────────
        var allWards = new List<GhnWardItem>();
        foreach (var district in allDistricts)
        {
            var wards = await FetchWardsAsync(http, district.DistrictId, ct);
            allWards.AddRange(wards);
        }

        // ──── 4. Write files ─────────────────────────────────────────────────
        await WriteJsonAsync("provinces.json", provinces.Select(p => new
        {
            ProvinceID   = p.ProvinceId,
            ProvinceName = p.ProvinceName
        }), ct);

        await WriteJsonAsync("districts.json", allDistricts.Select(d => new
        {
            DistrictID   = d.DistrictId,
            DistrictName = d.DistrictName,
            ProvinceID   = d.ProvinceId
        }), ct);

        await WriteJsonAsync("wards.json", allWards.Select(w => new
        {
            WardCode  = w.WardCode,
            WardName  = w.WardName,
            DistrictID = w.DistrictId
        }), ct);

        return new GhnSyncResult
        {
            ProvinceCount = provinces.Count,
            DistrictCount = allDistricts.Count,
            WardCount     = allWards.Count,
            SyncedAt      = DateTime.UtcNow
        };
    }

    // ─── Private helpers ─────────────────────────────────────────────────────

    private async Task<List<GhnProvinceItem>> FetchProvincesAsync(HttpClient http, CancellationToken ct)
    {
        var response = await http.GetFromJsonAsync<GhnProvinceListResponse>(
            "/shiip/public-api/master-data/province", _readOptions, ct);

        return response?.Data ?? [];
    }

    private async Task<List<GhnDistrictItem>> FetchDistrictsAsync(HttpClient http, int provinceId, CancellationToken ct)
    {
        var response = await http.PostAsJsonAsync(
            "/shiip/public-api/master-data/district",
            new { province_id = provinceId },
            ct);

        if (!response.IsSuccessStatusCode) return [];

        var body = await response.Content.ReadAsStringAsync(ct);
        var result = JsonSerializer.Deserialize<GhnDistrictListResponse>(body, _readOptions);
        return result?.Code == 200 ? (result.Data ?? []) : [];
    }

    private async Task<List<GhnWardItem>> FetchWardsAsync(HttpClient http, int districtId, CancellationToken ct)
    {
        try
        {
            var response = await http.GetFromJsonAsync<GhnWardListResponse>(
                $"/shiip/public-api/master-data/ward?district_id={districtId}", _readOptions, ct);

            return response?.Code == 200 ? (response.Data ?? []) : [];
        }
        catch
        {
            return [];
        }
    }

    private async Task WriteJsonAsync<T>(string fileName, IEnumerable<T> data, CancellationToken ct)
    {
        var finalPath = Path.Combine(_dataDir, fileName);
        var tempPath  = finalPath + ".tmp";

        // Write to a temp file first — readers never touch .tmp
        await using (var stream = File.Open(tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            await JsonSerializer.SerializeAsync(stream, data.ToList(), _writeOptions, ct);
        }

        // Atomic replace: on the same volume this is a rename, not a copy
        // Any concurrent reader either sees the old file or the new file — never a partial write
        File.Move(tempPath, finalPath, overwrite: true);
    }

    private HttpClient BuildClient()
    {
        var http = _httpClientFactory.CreateClient("GhnAddressClient");
        http.BaseAddress = new Uri(_baseUrl);
        http.DefaultRequestHeaders.Clear();
        http.DefaultRequestHeaders.Add("Token", _token);
        http.Timeout = TimeSpan.FromMinutes(30); // ward sync can take several minutes
        return http;
    }
}
