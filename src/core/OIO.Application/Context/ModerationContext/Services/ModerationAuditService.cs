using System.Security.Claims;
using System.Text.Json;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Data;
using OIO.Application.Context.UserContext.Services;
using OIO.Domain.Context.ModerationContext.Aggregates;

namespace OIO.Application.Context.ModerationContext.Services;

internal sealed class ModerationAuditService(
    IDbContext dbContext,
    IClock clock,
    ICurrentUser currentUser)
{
    public void Log(
        string action,
        string entityType,
        Guid? entityId,
        object? oldData = null,
        object? newData = null)
    {
        var role = currentUser.Claims
            .FirstOrDefault(x => x.Type == ClaimTypes.Role || x.Type == "role")
            ?.Value;

        var audit = AuditLog.Create(
            actorUserId: currentUser.UserId,
            actorRole: role,
            action: action,
            entityType: entityType,
            entityId: entityId,
            oldData: oldData is null ? null : JsonSerializer.Serialize(oldData),
            newData: newData is null ? null : JsonSerializer.Serialize(newData),
            ipAddress: null,
            nowUtc: clock.UtcNow);

        dbContext.Insert(audit);
    }
}
