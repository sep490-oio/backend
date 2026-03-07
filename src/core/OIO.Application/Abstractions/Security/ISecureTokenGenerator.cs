namespace OIO.Application.Abstractions.Security;

public interface ISecureTokenGenerator
{
    string GenerateToken(int byteLength = 32);
    string HashToken(string token);
}