namespace OIO.Application.Abstractions.Mail;

public interface IUserMailNotifier
{
    Task SendWelcomeVerifyAsync(
        string toEmail,
        string userName,
        string token,
        string userId,
        CancellationToken cancellationToken = default);

    Task SendPasswordResetAsync(
        string toEmail, 
        string userName,
        string token,
        CancellationToken cancellationToken = default);

    Task SendResendVerifyAsync(
        string toEmail, 
        string userName,
        string token, 
        string userId,
        CancellationToken cancellationToken = default);

    // ==================== Security ====================
    Task SendPasswordChangedAlertAsync(
        string toEmail, 
        string userName,
        CancellationToken cancellationToken = default);

}