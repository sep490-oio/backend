namespace OIO.Infrastructure.Mail.RazorEmails.Rendering.ViewModels;

public class WelcomeMailViewModel
{
    public required string UserName { get; set; }
    public required string VerifyLink { get; set; }
}

public class PasswordResetMailViewModel
{
    public required string UserName { get; set; }
    public required string ResetLink { get; set; }
    public required int ExpirationMinutes { get; set; }
}

public class PhoneVerificationMailViewModel
{
    public required string UserName { get; set; }
    public required string OtpCode { get; set; }
    public required int ExpirationMinutes { get; set; }
}