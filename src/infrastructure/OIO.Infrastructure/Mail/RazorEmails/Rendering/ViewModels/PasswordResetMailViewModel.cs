namespace OIO.Infrastructure.Mail.RazorEmails.Rendering.ViewModels;

public class PasswordResetMailViewModel
{
    public required string UserName { get; set; }
    public required string ResetLink { get; set; }
    public required int ExpirationHours { get; set; }
}