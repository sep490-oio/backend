using OIO.Application.Abstractions.Commons;
using OIO.Application.Abstractions.Settings;

namespace OIO.Infrastructure.Settings.Apps.Bride;

internal sealed class AuctionConfigsBridge : IAuctionConfigs
{
    private readonly ISystemSettingsService _settings;
    private readonly AuctionDefaults _defaults;

    public AuctionConfigsBridge(ISystemSettingsService settings, AuctionDefaults defaults)
    {
        _settings = settings;
        _defaults = defaults;
    }

    public async Task<int> GetMaxExtensionsPerAuctionAsync(CancellationToken cancellationToken = default) =>
        await _settings.GetAsync(
            SettingKeys.AuctionMaxExtensions,
            _defaults.MaxExtensionsPerAuction,
            cancellationToken);

    public async Task<TimeSpan> GetExtensionThresholdMinutesAsync(CancellationToken cancellationToken = default) =>
        await _settings.GetAsync(
            SettingKeys.AuctionExtensionThreshold,
            _defaults.ExtensionThresholdMinutes,
            cancellationToken);

    public async Task<TimeSpan> GetMaxDurationAsync(CancellationToken cancellationToken = default) =>
        await _settings.GetAsync(
            SettingKeys.AuctionMaxDuration,
            _defaults.MaxDuration,
            cancellationToken);

    public async Task<TimeSpan> GetMinDurationAsync(CancellationToken cancellationToken = default) =>
        await _settings.GetAsync(
            SettingKeys.AuctionMinDuration,
            _defaults.MinDuration,
            cancellationToken);
}