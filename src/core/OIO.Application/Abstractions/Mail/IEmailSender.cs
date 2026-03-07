namespace OIO.Application.Abstractions.Mail;

public interface IMailSender
{
    Task<bool> SendAsync(MailContent content, CancellationToken ct = default);
}