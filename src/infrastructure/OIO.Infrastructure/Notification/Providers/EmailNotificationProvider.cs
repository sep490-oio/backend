using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Mail;
using OIO.Application.Context.NotificationContext.Services;
using OIO.Domain.Context.NotificationContext.Aggregates;
using OIO.Infrastructure.Persistence;

namespace OIO.Infrastructure.Notification.Providers;

internal sealed class EmailNotificationProvider : INotificationProvider
{
    private readonly IMailSender _mailSender;
    private readonly ApplicationDbContext _dbContext;
    
    public EmailNotificationProvider(IMailSender mailSender, ApplicationDbContext dbContext)
    {
        _mailSender = mailSender;
        _dbContext = dbContext;
    }

    public string ChannelType => "Email";

    public async Task<Result> SendAsync(
        OIO.Domain.Context.NotificationContext.Aggregates.Notification notification, 
        NotificationDelivery delivery, 
        CancellationToken ct = default)
    {
        var user = await _dbContext.Set<OIO.Domain.Context.UserContext.Aggregates.Users.User>()
            .FirstOrDefaultAsync(u => u.Id == notification.UserId, ct);

        if (user == null)
            return Result.Failure("User not found");

        try
        {
            var content = new MailContent(
                user.Email,
                $"OIO Notification: {notification.Title}",
                $"<p>{notification.Message}</p>"
            );
            var success = await _mailSender.SendAsync(content, ct);
            return success ? Result.Success() : Result.Failure("MailSender returned false");
        }
        catch (Exception ex)
        {
            return Result.Failure(ex.Message);
        }
    }
}
