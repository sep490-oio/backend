using CSharpFunctionalExtensions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.NotificationContext.Services;
using OIO.Domain.Context.NotificationContext.Aggregates;
using OIO.Domain.Context.UserContext.Aggregates.Users;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.NotificationContext.Commands.CreateNotification;

internal sealed class CreateNotificationCommandHandler 
    : ICommandHandler<CreateNotificationCommand, Guid>
{
    private readonly IDbContext _dbContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationRoutingService _routingService;
    private readonly ILogger<CreateNotificationCommandHandler> _logger;

    public CreateNotificationCommandHandler(
        IDbContext dbContext,
        IUnitOfWork unitOfWork,
        INotificationRoutingService routingService,
        ILogger<CreateNotificationCommandHandler> logger)
    {
        _dbContext = dbContext;
        _unitOfWork = unitOfWork;
        _routingService = routingService;
        _logger = logger;
    }

    public async Task<Result<Guid, Error>> Handle(
        CreateNotificationCommand request,
        CancellationToken cancellationToken)
    {
        var notification = Notification.Create(
            userId: request.UserId,
            notificationType: request.NotificationType,
            eventType: request.EventType,
            title: request.Title,
            message: request.Message,
            priority: request.Priority,
            entityType: request.EntityType,
            entityId: request.EntityId,
            metadata: request.Metadata,
            relatedEntities: request.RelatedEntities,
            actions: request.Actions,
            expiresAt: request.ExpiresAt);

        _dbContext.Insert(notification);
        
        var userId = UserId.From(request.UserId);
        
        var preference = await _dbContext.Set<UserNotificationPreference>()
            .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);
        
        var deliveries = _routingService.Route(notification, preference).ToList();

        if (deliveries.Any())
        {
            _dbContext.InsertRange<NotificationDelivery>(deliveries);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Created notification {NotificationId} for user {UserId}",
            notification.Id.Value, notification.UserId.Value);

        return notification.Id.Value;
    }
}
