using OIO.Application.Abstractions.Commons;
using OIO.Application.Abstractions.Settings;

namespace OIO.Infrastructure.Settings.Apps.Bride;

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