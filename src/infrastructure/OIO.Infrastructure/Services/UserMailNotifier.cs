using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Commons;
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
    private readonly IAppConfigs _appConfigs;
    
    public UserMailNotifier(
        IOptionsMonitor<AppInfoOptions> options,
        IMailSender mailSender,
        RazorViewRenderer renderer,
        IClock clock,
        IAppConfigs appConfigs,
        ILogger<UserMailNotifier> logger)
    {
        _options = options.CurrentValue;
        _mailSender = mailSender;
        _renderer = renderer;
        _clock = clock;
        _appConfigs = appConfigs;
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
       
        var tokenExpirationMinutes = await _appConfigs.Auth.GetEmailVerificationTokenExpirationMinutesAsync(cancellationToken);
        
        var html = await _renderer.Render<WelcomeMail, WelcomeMailViewModel>(
            new WelcomeMailViewModel
            {
                UserName = userName,
                VerifyLink = verifyLink,
                ExpirationHours = tokenExpirationMinutes.Hours 
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
        
        var tokenExpirationMinutes = await _appConfigs.Auth.GetPasswordResetTokenExpirationMinutesAsync(cancellationToken);

        
        var html = await _renderer.Render<PasswordResetMail, PasswordResetMailViewModel>(
            new PasswordResetMailViewModel
            {
                UserName = userName,
                ResetLink = resetLink,
                ExpirationHours = tokenExpirationMinutes.Hours
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
        
        var tokenExpirationMinutes = await _appConfigs.Auth.GetEmailVerificationTokenExpirationMinutesAsync(cancellationToken);

        var html = await _renderer.Render<ResendVerifyMail, ResendVerifyMailViewModel>(
            new ResendVerifyMailViewModel
            {
                UserName = userName,
                VerifyUrl = verifyLink,
                ExpirationHours = tokenExpirationMinutes.Hours
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