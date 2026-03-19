using OIO.Domain.Context.UserContext.ValueObjects;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;

namespace OIO.Application.Context.UserContext.Services;

public interface IJwtTokenProvider
{
    string GenerateJwt(
        UserId userId,
        UserEmail email,
        UserName userName,
        Guid deviceId,
        IReadOnlyCollection<string> roles,
        DateTime now);
    
    string Generate();

    string GenerateTwoFactorJwt(UserId userId, DateTime now);
}