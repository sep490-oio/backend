using OIO.Domain.SeedWork.Errors;

namespace OIO.Domain.SeedWork.Exceptions;

public sealed class UnauthorizeException : DomainException
{
    public UnauthorizeException(Error error) :  base(error){}
    public UnauthorizeException(Error error, Exception innerException) :  base(error, innerException){}

    public static void ThrowWithError(Error error)
    {
        throw new UnauthorizeException(error);
    }
}