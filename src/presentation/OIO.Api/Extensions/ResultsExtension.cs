using Microsoft.AspNetCore.Http.HttpResults;
using OIO.Domain.SeedWork.Checks;
using OIO.Domain.SeedWork.Errors;
using OIO.Domain.SeedWork.Errors.ErrorCatalogs;

namespace OIO.Api.Extensions;

public static class ErrorExtension
{
    extension(Error error)
    {
        public IResult ProcessError()
        {
            
            if (error is ViolationsError violationsError)
            {
               return ToValidationProblem(violationsError);
            }

            var responseResult = TypedResults.Problem(
                statusCode: GetStatus(error),
                title: error.Message,
                detail: error.Code);
            
            return responseResult;
        }
    }

    private static int GetStatus(Error error)
    {
        return error.Kind switch
        {
            ErrorCatalog.Kind.Conflict => StatusCodes.Status409Conflict,
            ErrorCatalog.Kind.Validation => StatusCodes.Status400BadRequest,
            ErrorCatalog.Kind.Unauthorized => StatusCodes.Status401Unauthorized,
            ErrorCatalog.Kind.Forbidden => StatusCodes.Status403Forbidden,
            ErrorCatalog.Kind.NotFound => StatusCodes.Status404NotFound,
            ErrorCatalog.Kind.Invariant => StatusCodes.Status422UnprocessableEntity,
            ErrorCatalog.Kind.Violations => StatusCodes.Status422UnprocessableEntity,
            ErrorCatalog.Kind.Unexpected => StatusCodes.Status500InternalServerError,
            ErrorCatalog.Kind.Unavailable => StatusCodes.Status503ServiceUnavailable,
            ErrorCatalog.Kind.Timeout => StatusCodes.Status504GatewayTimeout,
            _ => StatusCodes.Status500InternalServerError
        };
    }

    private static ValidationProblem ToValidationProblem(ViolationsError error)
    {
        var errorsDict = error.Violations.GroupBy(e => ((ICheckError)e).PropertyName)
            .ToDictionary(g => g.Key, 
                g => 
                    g.Select(e => e.Message).ToArray());
        
        var validationProblem = TypedResults.ValidationProblem(errorsDict,
            title: error.Message,
            detail: error.Code);
        
        return validationProblem;
    }
}