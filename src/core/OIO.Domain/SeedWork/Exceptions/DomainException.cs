using CSharpFunctionalExtensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Domain.SeedWork.Exceptions;

public class DomainException : Exception 
{
    public Error Error { get; }

    public DomainException(Error error)
        : base(error.Message)
    {
        Error = error;
    }

    public DomainException(Error error, Exception innerException)
        : base(error.Message, innerException)
    {
        Error = error;
    }

    public static void ThrowIf(IUnitResult<Error> result)
    {
        if(result.IsFailure)
        {
            throw new DomainException(result.Error);
        }
    }
    public static void Throw(Error error)
    {
        throw new DomainException(error);
    }
}