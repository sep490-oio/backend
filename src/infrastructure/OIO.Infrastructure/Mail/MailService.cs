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
    private readonly EmailOptions _options;
    private readonly ILogger<MailSender> _logger;

    public MailSender(
        IOptions<EmailOptions> options,
        ILogger<MailSender> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task<bool> SendAsync(MailContent message, CancellationToken ct = default)
    {
        var mimeMessage = new MimeMessage();

        mimeMessage.From.Add(new MailboxAddress(_options.FromName, _options.FromAddress));
        mimeMessage.To.Add(MailboxAddress.Parse(message.To));
        mimeMessage.Subject = message.Subject;

        mimeMessage.Body = new BodyBuilder
        {
            HtmlBody = message.HtmlBody
        }.ToMessageBody();

        using var client = new SmtpClient();

        try
        {
            var secureOption = _options.UseStartTls
                ? SecureSocketOptions.StartTls
                : SecureSocketOptions.Auto;

            await client.ConnectAsync(_options.Host, _options.Port, secureOption, ct);

            if (!string.IsNullOrWhiteSpace(_options.Username))
            {
                await client.AuthenticateAsync(_options.Username, _options.Password, ct);
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