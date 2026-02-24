namespace OIO.Domain.Context.UserContext.Services;

public interface ITokenHasher
{
    string Hash(string token);
    bool Verify(string token, string hashedToken);
}