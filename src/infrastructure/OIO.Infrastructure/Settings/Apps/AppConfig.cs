using Microsoft.Extensions.Options;
using OIO.Application.Abstractions.Commons;
using OIO.Application.Abstractions.Settings;
using OIO.Infrastructure.Settings.Apps.Bride;

namespace OIO.Infrastructure.Settings.Apps;

public sealed class AppConfig : IAppConfigs
{
    private readonly IOptionsMonitor<AppInfoOptions> _monitor;
    private readonly ISystemSettingsService _settings;

    private AppInfoOptions Opts => _monitor.CurrentValue;

    public AppConfig(
        IOptionsMonitor<AppInfoOptions> monitor,
        ISystemSettingsService settings)
    {
        _monitor = monitor;
        _settings = settings;
    }

    public string AppName => Opts.AppName;
    public string Version => Opts.Version;
    public string FeUrl => Opts.FeUrl;
    public string BeUrl => Opts.BeUrl;
    public string EmailVerifyPath => Opts.EmailVerifyPath;
    public string ResetPasswordPath => Opts.ResetPasswordPath;
    public IFeatureConfigs Features => Opts.Features;

    // ===== Business (SystemSettings DB + cache) =====
    public IAuctionConfigs Auctions => new AuctionConfigsBridge(_settings, Opts.AuctionDefaults);
    public IItemConfigs Items => new ItemConfigsBridge(_settings, Opts.ItemDefaults);
    public IMediaConfigs Media => new MediaConfigsBridge(_settings, Opts.MediaDefaults);
    public IAuthConfigs Auth => new AuthConfigsBridge(_settings, Opts.AuthDefaults);
}