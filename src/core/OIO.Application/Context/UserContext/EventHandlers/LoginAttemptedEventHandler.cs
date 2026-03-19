using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using OIO.Application.Abstractions.Caching;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Data;
using OIO.Application.Context.NotificationContext;
using OIO.Application.Context.NotificationContext.Commands.CreateNotification;
using OIO.Domain.Context.ModerationContext.Aggregates;
using OIO.Domain.Context.ModerationContext.Enums;
using OIO.Domain.Context.NotificationContext.Enums;
using OIO.Domain.Context.UserContext.Aggregates.Users;
using OIO.Domain.Context.UserContext.Aggregates.Users.Events;
using OIO.Domain.Context.UserContext.Enums;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;

namespace OIO.Application.Context.UserContext.EventHandlers;

internal sealed class LoginAttemptedEventHandler
    : INotificationHandler<LoginAttemptedEvent>
{
    private const int FailedAttemptThreshold = 5;

    private readonly ILogger<LoginAttemptedEventHandler> _logger;
    private readonly IDbContext _dbContext;
    private readonly ISender _sender;
    private readonly HybridCache _cache;
    private readonly IClock _clock;
    private readonly IUnitOfWork _unitOfWork;

    public LoginAttemptedEventHandler(
        ILogger<LoginAttemptedEventHandler> logger,
        IDbContext dbContext,
        ISender sender,
        HybridCache cache,
        IClock clock,
        IUnitOfWork unitOfWork)
    {
        _logger = logger;
        _dbContext = dbContext;
        _sender = sender;
        _cache = cache;
        _clock = clock;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(LoginAttemptedEvent notification, CancellationToken cancellationToken)
    {
        if (notification.IsSuccess)
        {
            _logger.LogInformation(
                "Successful login for user {UserId} from {IpAddress}",
                notification.UserId,
                notification.IpAddress);
        }
        else
        {
            _logger.LogWarning(
                "Failed login attempt for user {UserId} from {IpAddress}",
                notification.UserId,
                notification.IpAddress);
        }

        if (!Guid.TryParse(notification.UserId, out var userIdGuid))
            return;

        var userId = UserId.From(userIdGuid);

        if (notification.IsSuccess)
        {
            await DetectSuspiciousLoginAsync(userId, notification, cancellationToken);
        }
        else
        {
            await TrackFailedLoginRateAsync(notification.IpAddress, userIdGuid, cancellationToken);
        }
    }

    private async Task DetectSuspiciousLoginAsync(
        UserId userId,
        LoginAttemptedEvent evt,
        CancellationToken ct)
    {
        var cacheKey = $"known_login_ips:{userId.Value}";
        var (exists, knownIps) = await _cache.TryGetValueAsync<HashSet<string>>(cacheKey, ct);

        if (!exists || knownIps is null)
        {
            knownIps = (await _dbContext.Set<UserLoginHistory>()
                    .AsNoTracking()
                    .Where(h => h.UserId == userId && h.Status == LoginStatus.Success)
                    .Select(h => h.IpAddress.ToString())
                    .Distinct()
                    .ToListAsync(ct))
                .ToHashSet();
        }

        if (knownIps.Contains(evt.IpAddress))
            return;

        knownIps.Add(evt.IpAddress);
        await _cache.SetAsync(cacheKey, knownIps,
            new HybridCacheEntryOptions { Expiration = TimeSpan.FromHours(24) },
            cancellationToken: ct);

        var alert = MonitoringAlert.Create(
            entityType: "User",
            entityId: userId.Value,
            alertType: "suspicious_login_new_ip",
            severity: AlertSeverity.Medium,
            payload: JsonSerializer.Serialize(new
            {
                ipAddress = evt.IpAddress,
                userAgent = evt.UserAgent,
                occurredAt = evt.OccurredAt
            }),
            nowUtc: _clock.UtcNow);

        _dbContext.Insert(alert);
        await _unitOfWork.SaveChangesAsync(ct);

        await NotificationDispatch.DispatchAsync(
            _sender, _logger,
            new CreateNotificationCommand(
                UserId: userId.Value,
                NotificationType: "security",
                EventType: "suspicious_login_new_ip",
                Title: "Dang nhap tu IP moi",
                Message:
                $"Tai khoan cua ban vua duoc dang nhap tu dia chi IP {evt.IpAddress}. Neu khong phai ban, hay doi mat khau ngay.",
                Priority: NotificationPriority.High,
                EntityType: "User",
                EntityId: userId.Value,
                Metadata: NotificationDispatch.SerializeMetadata(new
                {
                    ipAddress = evt.IpAddress,
                    userAgent = evt.UserAgent
                })),
            ct);

        _logger.LogInformation(
            "Suspicious login detected: new IP {IpAddress} for user {UserId}",
            evt.IpAddress, userId.Value);
    }

    private async Task TrackFailedLoginRateAsync(
        string ipAddress,
        Guid userId,
        CancellationToken ct)
    {
        var cacheKey = $"login_failed:{ipAddress}";
        var (exists, count) = await _cache.TryGetValueAsync<int>(cacheKey, ct);
        var newCount = exists ? count + 1 : 1;

        await _cache.SetAsync(cacheKey, newCount,
            new HybridCacheEntryOptions { Expiration = TimeSpan.FromHours(1) },
            cancellationToken: ct);

        if (newCount == FailedAttemptThreshold)
        {
            var alert = MonitoringAlert.Create(
                entityType: "User",
                entityId: userId,
                alertType: "login_brute_force",
                severity: AlertSeverity.High,
                payload: JsonSerializer.Serialize(new
                {
                    ipAddress,
                    failedAttempts = newCount,
                    windowHours = 1
                }),
                nowUtc: _clock.UtcNow);

            _dbContext.Insert(alert);
            await _unitOfWork.SaveChangesAsync(ct);

            _logger.LogWarning(
                "Brute force detected: {FailedAttempts} failed login attempts from IP {IpAddress} in last hour",
                newCount, ipAddress);
        }
    }
}
