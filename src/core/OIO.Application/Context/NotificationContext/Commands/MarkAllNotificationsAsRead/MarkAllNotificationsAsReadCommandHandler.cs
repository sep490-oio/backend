using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.UserContext.Services;
using OIO.Domain.Context.NotificationContext.Aggregates;
using OIO.Domain.Context.NotificationContext.Enums;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.NotificationContext.Commands.MarkAllNotificationsAsRead;

internal sealed class MarkAllNotificationsAsReadCommandHandler 
    : ICommandHandler<MarkAllNotificationsAsReadCommand, MarkAllNotificationsAsReadResponse>
{
    private readonly IDbContext _dbContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IClock _clock;
    private readonly ILogger<MarkAllNotificationsAsReadCommandHandler> _logger;

    public MarkAllNotificationsAsReadCommandHandler(
        IDbContext dbContext,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        IClock clock,
        ILogger<MarkAllNotificationsAsReadCommandHandler> logger)
    {
        _dbContext = dbContext;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _clock = clock;
        _logger = logger;
    }

    public async Task<Result<MarkAllNotificationsAsReadResponse, Error>> Handle(
        MarkAllNotificationsAsReadCommand request,
        CancellationToken cancellationToken)
    {
        
        // Load all unread notifications for this user
        var query = _dbContext.Set<Notification>()
            .Where(n => n.UserId == _currentUser.UserId && n.Status == NotificationStatus.Unread);

        var unreadNotifications = await query.ToListAsync(cancellationToken);

        if (unreadNotifications.Count == 0)
        {
            return new MarkAllNotificationsAsReadResponse(0); // Nothing to update
        }

        var now = _clock.UtcNow;
        
        foreach (var notification in unreadNotifications)
        {
            notification.MarkAsRead(now);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Marked {Count} notifications as read for user {UserId}",
            unreadNotifications.Count, _currentUser.UserId);

        return new MarkAllNotificationsAsReadResponse(unreadNotifications.Count);
    }
}
