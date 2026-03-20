using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.UserContext.DTOs;
using OIO.Application.Context.UserContext.Mappings;
using OIO.Application.Context.UserContext.Services;
using OIO.Domain.Context.UserContext.Aggregates.Users;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.UserContext.Queries.GetNotificationPreference;

public sealed record GetNotificationPreferenceQuery : IQuery<UserNotificationPreferenceDto>;

internal sealed class GetNotificationPreferenceQueryHandler
    : IQueryHandler<GetNotificationPreferenceQuery, UserNotificationPreferenceDto>
{
    private readonly IDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public GetNotificationPreferenceQueryHandler(
        IDbContext dbContext,
        ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async Task<Result<UserNotificationPreferenceDto, Error>> Handle(
        GetNotificationPreferenceQuery request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;

        var preference = await _dbContext.Set<UserNotificationPreference>()
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);

        if (preference is null)
        {
            return new UserNotificationPreferenceDto(
                Id: Guid.Empty,
                IsEnabled: true,
                Channels: "{}",
                QuietHours: null,
                RateLimits: null,
                CreatedAt: DateTime.MinValue,
                ModifiedAt: null);
        }

        return preference.ToDto();
    }
}
