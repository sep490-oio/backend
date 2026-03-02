using OIO.Domain.SeedWork.Errors;

namespace OIO.Domain.SeedWork.Exceptions;

public abstract class DomainException : Exception
{
    public Error Error { get; }

    protected DomainException(Error error)
        : base(error.Message)
    {
        Error = error;
    }

    protected DomainException(Error error, Exception innerException)
        : base(error.Message, innerException)
    {
        Error = error;
    }
}