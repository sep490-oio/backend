namespace OIO.Application.Abstractions.Settings;

public interface ISystemSettingsService
{
    Task<T> GetAsync<T>(string key, T defaultValue, CancellationToken ct = default);
    Task SetAsync<T>(string key, T value, CancellationToken ct = default);
    Task InvalidateCacheAsync(string? key = null, CancellationToken ct = default);
}