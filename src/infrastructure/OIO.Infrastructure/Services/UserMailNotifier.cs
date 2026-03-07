using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Mail;
using OIO.Infrastructure.Mail.RazorEmails.Rendering;
using OIO.Infrastructure.Mail.RazorEmails.Rendering.ViewModels;
using OIO.Infrastructure.Mail.RazorEmails.Rendering.Views;
using OIO.Infrastructure.Settings;
using OIO.Infrastructure.Settings.Apps;

namespace OIO.Infrastructure.Services;

public class UserMailNotifier : IUserMailNotifier
{
    private readonly AppInfoOptions _options;
    private readonly IMailSender _mailSender;
    private readonly RazorViewRenderer _renderer;
    private readonly IClock _clock;
    private readonly ILogger<UserMailNotifier> _logger;
    
    public UserMailNotifier(
        IOptionsMonitor<AppInfoOptions> options,
        IMailSender mailSender,
        RazorViewRenderer renderer,
        IClock clock,
        ILogger<UserMailNotifier> logger)
    {
        _options = options.CurrentValue;
        _mailSender = mailSender;
        _renderer = renderer;
        _clock = clock;
        _logger = logger;
    }
    
    public async Task SendWelcomeVerifyAsync(
        string toEmail, 
        string userName,
        string token, 
        string userId,
        CancellationToken cancellationToken = default)
    {
        var verifyLink = BuildFrontendUrl(_options.EmailVerifyPath, new()
        {
            ["token"] = token,
            ["userId"] = userId
        });
        
        var html = await _renderer.Render<WelcomeMail, WelcomeMailViewModel>(
            new WelcomeMailViewModel
            {
                UserName = userName,
                VerifyLink = verifyLink
            });
        
        var subject = $"Welcome to {_options.AppName}! Please verify your email";

        await SendAsync(
            to: toEmail,
            subject: subject,
            htmlBody: html, 
            cancellationToken: cancellationToken);
        
    }

    public async Task SendPasswordResetAsync(
        string toEmail,
        string userName,
        string token,
        CancellationToken cancellationToken = default)
    {
        var resetLink = BuildFrontendUrl(_options.ResetPasswordPath, new()
        {
            ["email"] = toEmail,
            ["token"] = token
        });
        
        var html = await _renderer.Render<PasswordResetMail, PasswordResetMailViewModel>(
            new PasswordResetMailViewModel
            {
                UserName = userName,
                ResetLink = resetLink,
                ExpirationMinutes = 30 // TODO: from config
            });

        var subject = $"{_options.AppName} — Reset your password";

        await SendAsync(
            to: toEmail,
            subject: subject,
            htmlBody: html, 
            cancellationToken: cancellationToken);
    }

    public async Task SendResendVerifyAsync(
        string toEmail,
        string userName,
        string token,
        string userId,
        CancellationToken cancellationToken = default)
    {
        var verifyLink = BuildFrontendUrl(_options.EmailVerifyPath, new()
        {
            ["token"] = token,
            ["userId"] = userId
        });

        var html = await _renderer.Render<WelcomeMail, WelcomeMailViewModel>(
            new WelcomeMailViewModel
            {
                UserName = userName,
                VerifyLink = verifyLink
            });

        var subject = $"{_options.AppName} — Verify your email";
        
        await SendAsync(
            to: toEmail,
            subject: subject,
            htmlBody: html, 
            cancellationToken: cancellationToken);
    }

    public async Task SendPasswordChangedAlertAsync(
        string toEmail,
        string userName,
        CancellationToken cancellationToken = default)
    {
        var html = await _renderer.Render<PasswordChangedAlertMail, PasswordChangedAlertMailViewModel>(
            new PasswordChangedAlertMailViewModel
            {
                UserName = userName,
                ChangedAt = _clock.UtcNow
            });

        var subject = $"{_options.AppName} — Your password was changed";
        
        await SendAsync(
            to: toEmail,
            subject: subject,
            htmlBody: html, 
            cancellationToken: cancellationToken);
    }


    private async Task SendAsync(
        string to, 
        string subject,
        string htmlBody,
        CancellationToken cancellationToken)
    {
        var sent = await _mailSender.SendAsync(
            new MailContent(to, subject, htmlBody), 
            cancellationToken);

        if (!sent)
        {
            _logger.LogError("Failed to send email to {To}. Subject: {Subject}", to, subject);
            throw new InvalidOperationException($"Failed to send email to {to}");
        }

        _logger.LogInformation("Email sent to {To}. Subject: {Subject}", to, subject);
    }

    private string BuildFrontendUrl(
        string relativePath,
        Dictionary<string, string> queryParams)
    {
        var baseUrl = _options.FeUrl.TrimEnd('/');
        var path = relativePath.StartsWith('/') ? relativePath : $"/{relativePath}";
        var url = $"{baseUrl}{path}";

        foreach (var (key, value) in queryParams)
        {
            url = QueryHelpers.AddQueryString(url, key, value);
        }

        return url;
    }
}