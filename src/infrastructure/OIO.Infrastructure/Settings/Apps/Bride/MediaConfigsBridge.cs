using OIO.Application.Abstractions.Commons;
using OIO.Application.Abstractions.Settings;

namespace OIO.Infrastructure.Settings.Apps.Bride;

internal sealed class MediaConfigsBridge : IMediaConfigs
{
    private readonly ISystemSettingsService _settings;
    private readonly MediaDefaults _defaults;

    public MediaConfigsBridge(ISystemSettingsService settings, MediaDefaults defaults)
    {
        _settings = settings;
        _defaults = defaults;
    }

    public async Task<TimeSpan> GetSignatureExpirationMinutesAsync(CancellationToken cancellationToken = default) =>
        await _settings.GetAsync(
            SettingKeys.MediaSignatureExpiration, 
            _defaults.SignatureExpirationMinutes, 
            cancellationToken);

    public async Task<TimeSpan> GetOrphanExpirationMinutesAsync(CancellationToken cancellationToken = default) =>
        await _settings.GetAsync(
            SettingKeys.MediaOrphanExpiration, 
            _defaults.OrphanExpirationMinutes, 
            cancellationToken);

    public async Task<TimeSpan> GetLinkedRecordRetentionDaysAsync(CancellationToken cancellationToken = default) =>
        await _settings.GetAsync(
            SettingKeys.MediaLinkedRetention, 
            _defaults.LinkedRecordRetentionDays,
            cancellationToken);

    public async Task<TimeSpan> GetCleanupIntervalMinutesAsync(CancellationToken cancellationToken = default) =>
        await _settings.GetAsync(
            SettingKeys.MediaCleanupInterval, 
            _defaults.CleanupIntervalMinutes, 
            cancellationToken);

    public async Task<IReadOnlyList<UploadContextOption>> GetUploadContextsAsync(CancellationToken cancellationToken = default) =>
        MergeUploadContexts(
            await _settings.GetAsync<IReadOnlyList<UploadContextOption>>(
                SettingKeys.MediaUploadContexts,
                _defaults.UploadContexts,
                cancellationToken),
            _defaults.UploadContexts);

    private static IReadOnlyList<UploadContextOption> MergeUploadContexts(
        IReadOnlyList<UploadContextOption> configuredContexts,
        IReadOnlyList<UploadContextOption> defaultContexts)
    {
        var merged = configuredContexts
            .ToDictionary(x => x.Name, StringComparer.OrdinalIgnoreCase);

        foreach (var context in defaultContexts)
        {
            if (!merged.ContainsKey(context.Name))
                merged[context.Name] = context;
        }

        return merged.Values
            .OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
