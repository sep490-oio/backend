using System.Collections.Concurrent;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace OIO.Infrastructure.Authorizations;

internal sealed class PermissionAuthorizationPolicyProvider : DefaultAuthorizationPolicyProvider
{
    private readonly ConcurrentDictionary<string, AuthorizationPolicy> _policies = new(StringComparer.OrdinalIgnoreCase);

    public PermissionAuthorizationPolicyProvider(IOptions<AuthorizationOptions> options)
        : base(options)
    {
    }

    public override async Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        var policy = await base.GetPolicyAsync(policyName);

        if (policy is not null)
        {
            return policy;
        }

        return _policies.GetOrAdd(policyName, name =>
        {
            return new AuthorizationPolicyBuilder()
                .AddRequirements(new PermissionRequirement(name))
                .Build();
        });
    }
}