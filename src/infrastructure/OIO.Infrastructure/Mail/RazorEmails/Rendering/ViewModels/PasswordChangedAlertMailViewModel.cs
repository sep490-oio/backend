namespace OIO.Infrastructure.Mail.RazorEmails.Rendering.ViewModels;

public class PasswordChangedAlertMailViewModel
{
    public required string UserName { get; set; }
    public required DateTime ChangedAt { get; set; }
}