namespace OIO.Domain.SeedWork.Errors;

public class InvalidErrorException : Exception
{
    public InvalidErrorException(string message) : base(message){}
    public InvalidErrorException(string message, Exception inner) : base(message, inner){}
}