using CSharpFunctionalExtensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Domain.SeedWork.Extensions;

public static class ResultExtensions 
{
    extension<T>(Result<T, Error> result)
    {
        public Result<T, Error> WithError(Error error)
        {
            return result.IsFailure ? Result.Failure<T, Error>(error) : result;
        }
    }

    extension(Result)
    {
        public static UnitResult<Error> FirstError(params Error?[] errors) 
        {
            foreach (var error in errors)
            {
                if (error != null)
                {
                    return error;
                }
            }
            return UnitResult.Success<Error>();
        }
    }
    
}
