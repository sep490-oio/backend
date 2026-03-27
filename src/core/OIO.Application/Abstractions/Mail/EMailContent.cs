namespace OIO.Application.Abstractions.Mail;

public sealed record MailContent(string To, string Subject, string HtmlBody, string? TextBody = null);