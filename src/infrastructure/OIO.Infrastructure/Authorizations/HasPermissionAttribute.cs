using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using OIO.Application.UserContext.Services;
using OIO.Domain.Constants;

namespace OIO.Infrastructure.Authorizations;

internal sealed class PermissionRequirement : IAuthorizationRequirement
{
    public PermissionRequirement(string permission)
    {
        Permission = permission;
    }

    public string Permission { get; }
}

internal sealed class PermissionAuthorizationPolicyProvider : DefaultAuthorizationPolicyProvider
{
    private readonly AuthorizationOptions _authorizationOptions;

    public PermissionAuthorizationPolicyProvider(IOptions<AuthorizationOptions> options)
        : base(options)
    {
        _authorizationOptions = options.Value;
    }

    public override async Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        var policy = await base.GetPolicyAsync(policyName);

        if (policy is not null)
        {
            return policy;
        }

        var permissionPolicy = new AuthorizationPolicyBuilder()
            .AddRequirements(new PermissionRequirement(policyName))
            .Build();

        _authorizationOptions.AddPolicy(policyName, permissionPolicy);

        return permissionPolicy;
    }
}

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
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            return;

        if (_currentUser.IsInRole(AppRole.Admin))
        {
            context.Succeed(requirement);
        }
        else
        {
            var permissions = await _permissionProvider.GetPermissionsAsync(_currentUser.UserId);

            if (permissions.Contains(requirement.Permission))
                context.Succeed(requirement);
        }
        
    }
}
