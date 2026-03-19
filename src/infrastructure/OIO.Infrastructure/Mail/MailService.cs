using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using OIO.Application.Abstractions.Mail;
using OIO.Infrastructure.Settings;

namespace OIO.Infrastructure.Mail;

internal sealed class MailSender : IMailSender
{
    private readonly IOptionsMonitor<EmailOptions> _options;
    private readonly ILogger<MailSender> _logger;

    public MailSender(
        IOptionsMonitor<EmailOptions> options,
        ILogger<MailSender> logger)
    {
        _options = options;
        _logger = logger;
    }

    public async Task<bool> SendAsync(MailContent message, CancellationToken ct = default)
    {
        var mimeMessage = new MimeMessage();

        mimeMessage.From.Add(new MailboxAddress(_options.CurrentValue.FromName, _options.CurrentValue.FromAddress));
        mimeMessage.To.Add(MailboxAddress.Parse(message.To));
        mimeMessage.Subject = message.Subject;

        mimeMessage.Body = new BodyBuilder
        {
            HtmlBody = message.HtmlBody
        }.ToMessageBody();

        using var client = new SmtpClient();

        try
        {
            var secureOption = _options.CurrentValue.UseStartTls
                ? SecureSocketOptions.StartTls
                : SecureSocketOptions.Auto;

            await client.ConnectAsync(_options.CurrentValue.Host, _options.CurrentValue.Port, secureOption, ct);

            if (!string.IsNullOrWhiteSpace(_options.CurrentValue.Username))
            {
                await client.AuthenticateAsync(_options.CurrentValue.Username, _options.CurrentValue.Password, ct);
            }

            await client.SendAsync(mimeMessage, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            return false;
        }
        finally
        {
            // 7. Disconnect safely
            if (client.IsConnected)
            {
                await client.DisconnectAsync(true, ct);
            }
        }

        return true;
    }
}