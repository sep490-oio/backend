using OIO.Application.Abstractions.Commons;
using OIO.Application.Abstractions.Settings;

namespace OIO.Infrastructure.Settings.Apps.Bride;

internal sealed class ItemConfigsBridge : IItemConfigs
{
    private readonly ISystemSettingsService _settings;
    private readonly ItemDefaults _defaults;

    public ItemConfigsBridge(ISystemSettingsService settings, ItemDefaults defaults)
    {
        _settings = settings;
        _defaults = defaults;
    }

    public async Task<int> GetMaxQuestionsPerItemAsync(CancellationToken cancellationToken = default) =>
        await _settings.GetAsync(
            SettingKeys.ItemMaxQuestions, 
            _defaults.MaxQuestionsPerItem,
            cancellationToken);
}