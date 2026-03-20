using Microsoft.AspNetCore.Authorization;
using OIO.Application.Context.UserContext.Services;
using OIO.Domain.AppDefinitions;

namespace OIO.Infrastructure.Authorizations;

internal sealed class PermissionAuthorizationHandler
    : AuthorizationHandler<PermissionRequirement>
{
    private readonly IPermissionService _permissionProvider;
    private readonly ICurrentUser _currentUser;

    public PermissionAuthorizationHandler(IPermissionService permissionProvider, ICurrentUser currentUser)
    {
        _permissionProvider = permissionProvider;
        _currentUser = currentUser;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        if (!_currentUser.IsAuthenticated)
            return;

        var permissions = await _permissionProvider.GetPermissionsAsync(_currentUser.UserId);

        if (permissions.Contains(requirement.Permission))
            context.Succeed(requirement);
    }
}