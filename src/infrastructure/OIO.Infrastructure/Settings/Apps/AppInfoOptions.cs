using Microsoft.Extensions.Options;
using OIO.Application.Abstractions.Commons;
using OIO.Application.Abstractions.Settings;

namespace OIO.Infrastructure.Settings.Apps;

public sealed class AppInfoOptions 
{
    public const string SectionName = "AppInfo";

    public string AppName { get; set; } = "AppName";
    public string Version { get; set; } = "1.0.0";
    public string FeUrl { get; set; } = null!;
    public string BeUrl { get; set; } = null!;
    public string EmailVerifyPath { get; set; } = null!;
    public string ResetPasswordPath { get; set; } = null!;
    public Features Features { get; set; } = null!;
    
    public AuctionDefaults AuctionDefaults { get; set; } = new();
    public ItemDefaults ItemDefaults { get; set; } = new();
    public MediaDefaults MediaDefaults { get; set; } = new();
    public AuthDefaults AuthDefaults { get; set; } = new();

}

public sealed class Features : IFeatureConfigs
{
    public bool EnableScalar { get; set; } = false;
}

public sealed class AuctionDefaults
{
    public int MaxExtensionsPerAuction { get; set; } = 10;
    public TimeSpan ExtensionThresholdMinutes { get; set; } = TimeSpan.FromMinutes(5);
    public TimeSpan MaxDuration { get; set; } = TimeSpan.FromDays(30);
    public TimeSpan MinDuration { get; set; } = TimeSpan.FromDays(1);
}


public sealed class ItemDefaults
{
    public int MaxQuestionsPerItem { get; set; } = 100;
}

public sealed class MediaDefaults
{
    public TimeSpan SignatureExpirationMinutes { get; set; } = TimeSpan.FromMinutes(30);
    public TimeSpan OrphanExpirationMinutes { get; set; } = TimeSpan.FromMinutes(60);
    public TimeSpan LinkedRecordRetentionDays { get; set; } = TimeSpan.FromDays(7);
    public TimeSpan CleanupIntervalMinutes { get; set; } = TimeSpan.FromMinutes(15);
    
    public List<UploadContextOption> UploadContexts { get; set; } = [];
    
}

public sealed class AuthDefaults
{
    public TimeSpan PasswordResetTokenExpirationMinutes { get; set; } = TimeSpan.FromMinutes(30);
    public TimeSpan ResendEmailCooldownSeconds { get; set; } = TimeSpan.FromSeconds(60);
    public TimeSpan EmailVerificationTokenExpirationMinutes { get; set; } = TimeSpan.FromMinutes(30);
    public TimeSpan TwoFactorSetupTokenExpirationMinutes { get; set; } = TimeSpan.FromMinutes(30);
    public TimeSpan PhoneVerificationTokenExpirationMinutes { get; set; } = TimeSpan.FromMinutes(30);
    public int MaxPasswordResetAttemptsPerHour { get; set; } = 5;
}

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
       await _settings.GetAsync<IReadOnlyList<UploadContextOption>>(
            SettingKeys.MediaUploadContexts, 
            _defaults.UploadContexts,
            cancellationToken);
}

internal sealed class AuthConfigsBridge : IAuthConfigs
{
    private readonly ISystemSettingsService _settings;
    private readonly AuthDefaults _defaults;

    public AuthConfigsBridge(ISystemSettingsService settings, AuthDefaults defaults)
    {
        _settings = settings;
        _defaults = defaults;
    }

    public async Task<TimeSpan> GetPasswordResetTokenExpirationMinutesAsync(CancellationToken cancellationToken = default) =>
        await _settings.GetAsync(
            SettingKeys.AuthPasswordResetExpiration,
            _defaults.PasswordResetTokenExpirationMinutes,
            cancellationToken);

    public async Task<TimeSpan> GetResendEmailCooldownSecondsAsync(CancellationToken cancellationToken = default) =>
        await _settings.GetAsync(
            SettingKeys.AuthResendEmailCooldown,
            _defaults.ResendEmailCooldownSeconds,
            cancellationToken);
    
    public async Task<TimeSpan> GetTwoFactorSetupTokenExpirationMinutesAsync(CancellationToken cancellationToken = default) =>
        await _settings.GetAsync(
            SettingKeys.TwoFactorSetupTokenExpiration,
            _defaults.TwoFactorSetupTokenExpirationMinutes,
            cancellationToken);
    
    public async Task<TimeSpan> GetPhoneVerificationTokenExpirationMinutesAsync(CancellationToken cancellationToken = default) =>
        await _settings.GetAsync(
            SettingKeys.PhoneVerificationTokenExpiration,
            _defaults.PhoneVerificationTokenExpirationMinutes,
            cancellationToken);
    
    public async Task<TimeSpan> GetEmailVerificationTokenExpirationMinutesAsync(CancellationToken cancellationToken = default) =>
        await _settings.GetAsync(
            SettingKeys.EmailVerificationTokenExpiration,
            _defaults.EmailVerificationTokenExpirationMinutes,
            cancellationToken);

    public async Task<int> GetMaxPasswordResetAttemptsPerHourAsync(CancellationToken cancellationToken = default) =>
        await _settings.GetAsync(
            SettingKeys.AuthMaxPasswordResetAttempts,
            _defaults.MaxPasswordResetAttemptsPerHour,
            cancellationToken);
}