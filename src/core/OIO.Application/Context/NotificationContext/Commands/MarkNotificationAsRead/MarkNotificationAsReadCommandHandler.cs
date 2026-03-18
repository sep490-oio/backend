using CSharpFunctionalExtensions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.UserContext.Services;
using OIO.Domain.Context.NotificationContext.Aggregates;
using OIO.Domain.Context.NotificationContext.ValueObjects.Ids;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Errors;
using OIO.Domain.SeedWork.Errors.ErrorCatalogs;

namespace OIO.Application.Context.NotificationContext.Commands.MarkNotificationAsRead;

internal sealed class MarkNotificationAsReadCommandHandler 
    : ICommandHandler<MarkNotificationAsReadCommand, MarkNotificationAsReadResponse>
{
    private readonly IDbContext _dbContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<MarkNotificationAsReadCommandHandler> _logger;

    public MarkNotificationAsReadCommandHandler(
        IDbContext dbContext,
        IUnitOfWork unitOfWork,
        IClock clock,
        ICurrentUser currentUser,
        ILogger<MarkNotificationAsReadCommandHandler> logger)
    {
        _dbContext = dbContext;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<Result<MarkNotificationAsReadResponse, Error>> Handle(
        MarkNotificationAsReadCommand request,
        CancellationToken cancellationToken)
    {
        var notificationId = NotificationId.From(request.NotificationId);

        var notification = await _dbContext.GetByIdAsync<Notification, NotificationId>(
            notificationId,
            cancellationToken: cancellationToken);

        if (notification is null || notification.UserId != _currentUser.UserId)
        {
            return Error.NotFound("Notification.NotFound", $"Notification {request.NotificationId} not found");
        }

        notification.MarkAsRead(_clock.UtcNow);
        
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Marked notification {NotificationId} as read by user {UserId}",
            request.NotificationId, _currentUser.UserId);

        return new MarkNotificationAsReadResponse(request.NotificationId);
    }
}
