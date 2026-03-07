namespace OIO.Application.Context.UserContext.Services;

public interface ITokenExpirationSettings
{
    TimeSpan AccessTokenExpiration { get; }
    TimeSpan RefreshTokenExpiration { get; }
    TimeSpan FamilySlidingExpiration { get; }
    TimeSpan FamilyAbsoluteExpiration { get; }
}