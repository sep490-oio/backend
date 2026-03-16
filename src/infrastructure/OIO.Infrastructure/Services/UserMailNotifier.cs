using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Commons;
using OIO.Application.Abstractions.Mail;
using OIO.Infrastructure.Mail.RazorEmails.Rendering;
using OIO.Infrastructure.Mail.RazorEmails.Rendering.ViewModels;
using OIO.Infrastructure.Mail.RazorEmails.Rendering.Views;
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
        DateTime tokenExpiry,
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
                VerifyLink = verifyLink,
                ExpiredAt = tokenExpiry.ToString("dddd, dd MMMM yyyy hh:mm tt"),
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
        DateTime tokenExpiry,
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
                ExpiredAt = tokenExpiry.ToString("dddd, dd MMMM yyyy hh:mm tt")
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
        DateTime tokenExpiry,
        CancellationToken cancellationToken = default)
    {
        var verifyLink = BuildFrontendUrl(_options.EmailVerifyPath, new()
        {
            ["token"] = token,
            ["userId"] = userId
        });

        var html = await _renderer.Render<ResendVerifyMail, ResendVerifyMailViewModel>(
            new ResendVerifyMailViewModel
            {
                UserName = userName,
                VerifyUrl = verifyLink,
                ExpiredAt = tokenExpiry.ToString("dddd, dd MMMM yyyy hh:mm tt")
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

    public async Task SendEmailConfirmedAsync(
        string toEmail,
        string userName,
        CancellationToken cancellationToken = default)
    {
        var safeUserName = WebUtility.HtmlEncode(userName);
        var html =
            $"<p>Hello {safeUserName},</p>" +
            $"<p>Your email address has been verified successfully. You can now use all features that require a confirmed email on {_options.AppName}.</p>" +
            "<p>If you did not expect this change, please contact support immediately.</p>";

        var subject = $"{_options.AppName} - Email verified successfully";

        await SendAsync(
            to: toEmail,
            subject: subject,
            htmlBody: html,
            cancellationToken: cancellationToken);
    }

    public async Task SendAccountLockedAlertAsync(
        string toEmail,
        string userName,
        DateTime lockoutEnd,
        int failedAttempts,
        CancellationToken cancellationToken = default)
    {
        var safeUserName = WebUtility.HtmlEncode(userName);
        var html =
            $"<p>Hello {safeUserName},</p>" +
            $"<p>Your account has been temporarily locked after {failedAttempts} failed sign-in attempts.</p>" +
            $"<p>Lockout end: {lockoutEnd:yyyy-MM-dd HH:mm:ss} UTC.</p>" +
            "<p>If this was not you, please reset your password after the lockout period ends.</p>";

        var subject = $"{_options.AppName} - Security alert: account temporarily locked";

        await SendAsync(
            to: toEmail,
            subject: subject,
            htmlBody: html,
            cancellationToken: cancellationToken);
    }

    public async Task SendSecuritySessionRevokedAlertAsync(
        string toEmail,
        string userName,
        string reason,
        Guid deviceId,
        CancellationToken cancellationToken = default)
    {
        var safeUserName = WebUtility.HtmlEncode(userName);
        var safeReason = WebUtility.HtmlEncode(reason);
        var html =
            $"<p>Hello {safeUserName},</p>" +
            "<p>One of your sessions was revoked for security reasons.</p>" +
            $"<p>Device: {deviceId}</p>" +
            $"<p>Reason: {safeReason}</p>" +
            "<p>If this was not expected, please change your password and review your active sessions.</p>";

        var subject = $"{_options.AppName} - Security alert: session revoked";

        await SendAsync(
            to: toEmail,
            subject: subject,
            htmlBody: html,
            cancellationToken: cancellationToken);
    }

    public async Task SendAccountStatusChangedAsync(
        string toEmail,
        string userName,
        string oldStatus,
        string newStatus,
        CancellationToken cancellationToken = default)
    {
        var safeUserName = WebUtility.HtmlEncode(userName);
        var safeOldStatus = WebUtility.HtmlEncode(oldStatus);
        var safeNewStatus = WebUtility.HtmlEncode(newStatus);
        var html =
            $"<p>Hello {safeUserName},</p>" +
            $"<p>Your account status has changed from <strong>{safeOldStatus}</strong> to <strong>{safeNewStatus}</strong>.</p>" +
            "<p>If you believe this change is incorrect, please contact support.</p>";

        var subject = $"{_options.AppName} - Your account status has changed";

        await SendAsync(
            to: toEmail,
            subject: subject,
            htmlBody: html,
            cancellationToken: cancellationToken);
    }

    public async Task SendOutbidAsync(
        string toEmail,
        string userName,
        string auctionTitle, 
        decimal newPrice,
        string auctionUrl,
        string currency,
        CancellationToken cancellationToken = default)
    {
        var fullUrl = BuildFrontendUrl(auctionUrl);
        
        var html = await _renderer.Render<OutbidMail, OutbidMailViewModel>(
            new OutbidMailViewModel
            {
                UserName = userName,
                AuctionTitle = auctionTitle,
                NewHighestBid = newPrice,
                AuctionUrl = fullUrl,
                Currency = currency
            });
        
        var subject = $"{_options.AppName} - You've been outbid! — {auctionTitle}";
        
        await SendAsync(
            to: toEmail,
            subject: subject,
            htmlBody: html, 
            cancellationToken: cancellationToken);
    }

    public async Task SendAuctionWonAsync(
        string toEmail,
        string userName,
        string auctionId,
        string auctionTitle, 
        decimal finalPrice,
        string currency,
        string paymentUrl,
        CancellationToken cancellationToken = default)
    {
        //TODO FE should provide the payment url, but for now we build it here
        var payment = BuildFrontendUrl("orders/payment", new()
        {
            ["auctionId"] = auctionId,
        });
        
        var html = await _renderer.Render<AuctionWonMail, AuctionWonMailViewModel>(
            new AuctionWonMailViewModel
            {
                UserName = userName,
                AuctionId = auctionId,
                AuctionTitle = auctionTitle,
                FinalPrice = finalPrice,
                Currency = currency,
                PaymentUrl = paymentUrl,
                PaymentDeadlineHours = 48 //TODO: make it configurable
            });
        
        var subject = $"{_options.AppName} - Congratulations! You won the auction — {auctionTitle}";
        
        await SendAsync(
            to: toEmail,
            subject: subject,
            htmlBody: html, 
            cancellationToken: cancellationToken);
        
    }

    public async Task SendAuctionSoldAsync(
        string toEmail,
        string userName,
        string auctionId,
        string auctionTitle,
        string winnerName,
        decimal finalPrice,
        string currency,
        CancellationToken cancellationToken = default)
    {
        var html = await _renderer.Render<AuctionSoldMail, AuctionSoldMailViewModel>(
            new AuctionSoldMailViewModel
            {
                UserName = userName,
                AuctionId = auctionId,
                AuctionTitle = auctionTitle,
                FinalPrice = finalPrice,
                Currency = currency,
                WinnerName = winnerName
            });
        
        var subject = $"{_options.AppName} - Your item was sold! — {auctionTitle}";
        
        await SendAsync(
            to: toEmail,
            subject: subject,
            htmlBody: html, 
            cancellationToken: cancellationToken);
    }

    public async Task SendAuctionFailedAsync(
        string toEmail,
        string userName,
        string auctionId,
        string auctionTitle,
        string reason,
        decimal finalPrice,
        int totalBids, 
        string currency,
        CancellationToken cancellationToken = default)
    {
        var relistUrl = BuildFrontendUrl("items/relist", new()
        {
            ["auctionId"] = auctionId,
        });
        
        var html = await _renderer.Render<AuctionFailedMail, AuctionFailedMailViewModel>(
            new AuctionFailedMailViewModel()
            {
                UserName = userName,
                AuctionId = auctionId,
                AuctionTitle = auctionTitle,
                Reason = reason,
                FinalPrice = finalPrice,
                Currency = currency,
                TotalBids = totalBids,
                RelistUrl = relistUrl
            });
        
        var subject = $"{_options.AppName} - Your auction ended without a winner — {auctionTitle}";
        
        await SendAsync(
            to: toEmail,
            subject: subject,
            htmlBody: html, 
            cancellationToken: cancellationToken);
    }

    public async Task SendAuctionEndedWatcherAsync(
        string toEmail,
        string userName,
        string auctionId,
        string auctionTitle,
        decimal finalPrice,
        string currency, 
        bool hasWinner,
        CancellationToken cancellationToken = default)
    {
        var html = await _renderer.Render<AuctionEndedWatcherMail, AuctionEndedWatcherMailViewModel>(
            new AuctionEndedWatcherMailViewModel()
            {
                UserName = userName,
                AuctionId = auctionId,
                AuctionTitle = auctionTitle,
                FinalPrice = finalPrice,
                Currency = currency,
                HasWinner = hasWinner
            });
        
        var subject = $"{_options.AppName} - An auction you watched has ended — {auctionTitle}";
        
        await SendAsync(
            to: toEmail,
            subject: subject,
            htmlBody: html, 
            cancellationToken: cancellationToken);
    }

    public async Task SendAuctionCancelledAsync(
        string toEmail,
        string userName,
        string auctionId,
        string auctionTitle,
        string reason,
        CancellationToken cancellationToken = default)
    {
        var html = await _renderer.Render<AuctionCancelledMail, AuctionCancelledMailViewModel>(
            new AuctionCancelledMailViewModel()
            {
                UserName = userName,
                AuctionId = auctionId,
                AuctionTitle = auctionTitle,
                Reason = reason
            });
        
        var subject = $"{_options.AppName} - An auction you participated in has been cancelled — {auctionTitle}";
        
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
            _logger.LogError("Failed to send email to {To}. Subject: {Subject}", to, cancellationToken);
            throw new InvalidOperationException($"Failed to send email to {to}");
        }
        
        _logger.LogInformation("Email sent to {To}. Subject: {Subject}", to, cancellationToken);
    }

    private string BuildFrontendUrl(
        string relativePath,
        Dictionary<string, string>? queryParams = null)
    {
        var baseUrl = _options.FeUrl.TrimEnd('/');
        var path = relativePath.StartsWith('/') ? relativePath : $"/{relativePath}";
        var url = $"{baseUrl}{path}";

        if (queryParams == null) 
            return url;
        
        foreach (var (key, value) in queryParams)
        {
            url = QueryHelpers.AddQueryString(url, key, value);
        }

        return url;
    }
}
