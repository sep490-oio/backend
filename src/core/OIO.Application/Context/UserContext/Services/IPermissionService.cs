using OIO.Domain.Context.UserContext.ValueObjects.Ids;

namespace OIO.Application.Context.UserContext.Services;

public interface IPermissionService
{
    Task<HashSet<string>> GetPermissionsAsync(UserId userId, CancellationToken cancellationToken = default);
    Task InvalidatePermissionsCacheAsync(UserId userId, CancellationToken cancellationToken = default);
}
