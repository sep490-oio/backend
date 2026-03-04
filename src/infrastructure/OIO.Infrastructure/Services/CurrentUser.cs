using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.JsonWebTokens;
using OIO.Application.UserContext.Services;
using OIO.Domain.Context.UserContext.Errors;
using OIO.Domain.Context.UserContext.ValueObjects;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Exceptions;
using OIO.Infrastructure.Authorizations;

namespace OIO.Infrastructure.Services;

public sealed class CurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUser(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public UserId UserId
    {
        get
        {
            var idClaim = _httpContextAccessor.HttpContext?.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

            if (string.IsNullOrWhiteSpace(idClaim))
            {
                UnauthorizeException.ThrowWithError(UserErrors.Auth.UserNotLoggedIn);
            }
            
            return UserId.From(Guid.Parse(idClaim!)); 
        }
    }

    public UserName? UserName => UserName.Create(_httpContextAccessor.HttpContext?.User.FindFirst(JwtRegisteredClaimNames.Name)?.Value!).GetValueOrDefault();

    public UserEmail? Email => UserEmail.Create(_httpContextAccessor.HttpContext?.User.FindFirst(JwtRegisteredClaimNames.Email)?.Value!).GetValueOrDefault();

    public Guid DeviceId => Guid.Parse(_httpContextAccessor.HttpContext?.User.FindFirst(CustomClaimType.DeviceId)?.Value!);
    public Claim[] Claims => _httpContextAccessor.HttpContext?.User.Claims.ToArray()!;

    public bool IsAuthenticated => _httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated ?? false;

    public bool IsInRole(string roleName) =>
        _httpContextAccessor.HttpContext?.User.IsInRole(roleName.ToUpperInvariant()) ?? false;

}