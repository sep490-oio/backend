namespace OIO.Infrastructure.Mail.RazorEmails.Rendering.ViewModels;

public class WelcomeMailViewModel
{
    public required string UserName { get; set; }
    public required string VerifyLink { get; set; }
    public required int ExpirationHours { get; set; }
}