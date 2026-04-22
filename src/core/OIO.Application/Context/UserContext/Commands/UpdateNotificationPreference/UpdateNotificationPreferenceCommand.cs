using System.Text.Json;
using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.UserContext.DTOs;
using OIO.Application.Context.UserContext.Mappings;
using OIO.Application.Context.UserContext.Services;
using OIO.Domain.Context.NotificationContext.Enums;
using OIO.Domain.Context.UserContext.Aggregates.Users;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.UserContext.Commands.UpdateNotificationPreference;

public sealed record UpdateNotificationPreferenceCommand(
    bool IsEnabled,
    string Channels,
    string? QuietHours) : ICommand<UserNotificationPreferenceDto>, IHasValidate
{
    public ViolationsError Validate()
    {
        var violations = new ViolationsError(prefix: "UpdateNotificationPreference");

        string[] parsed;
        try
        {
            parsed = JsonSerializer.Deserialize<string[]>(Channels) ?? [];
        }
        catch (JsonException)
        {
            violations.Add(Error.Validation(
                "Preference.Channels",
                "UpdateNotificationPreference.MalformedChannels",
                "Malformed JSON"));
            return violations;
        }

        var allowed = NotificationChannel.All.Select(x => x.Id).ToHashSet(StringComparer.Ordinal);
        foreach (var entry in parsed)
        {
            if (!allowed.Contains(entry))
            {
                violations.Add(Error.Validation(
                    "Preference.Channels",
                    "UpdateNotificationPreference.UnknownChannel",
                    $"Unknown channel: {entry}"));
            }
        }

        if (IsEnabled && parsed.Length == 0)
        {
            violations.Add(Error.Validation(
                "Preference.Channels",
                "UpdateNotificationPreference.AtLeastOneChannel",
                "At least one channel required when notifications are enabled"));
        }

        return violations;
    }
}

internal sealed class UpdateNotificationPreferenceCommandHandler
    : ICommandHandler<UpdateNotificationPreferenceCommand, UserNotificationPreferenceDto>
{
    private readonly IDbContext _dbContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly ICurrentUser _currentUser;

    public UpdateNotificationPreferenceCommandHandler(
        IDbContext dbContext,
        IUnitOfWork unitOfWork,
        IClock clock,
        ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _currentUser = currentUser;
    }

    public async Task<Result<UserNotificationPreferenceDto, Error>> Handle(
        UpdateNotificationPreferenceCommand request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;
        var nowUtc = _clock.UtcNow;

        var preference = await _dbContext.Set<UserNotificationPreference>()
            .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);

        if (preference is null)
        {
            preference = UserNotificationPreference.Create(userId, nowUtc);
            _dbContext.Insert(preference);
        }

        preference.Update(request.IsEnabled, request.Channels, request.QuietHours, nowUtc);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return preference.ToDto();
    }
}
