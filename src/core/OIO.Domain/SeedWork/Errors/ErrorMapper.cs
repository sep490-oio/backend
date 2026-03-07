using CSharpFunctionalExtensions.HttpResults;
using Microsoft.AspNetCore.Http.HttpResults;

namespace OIO.Domain.SeedWork.Errors;

public class ErrorMapper : IResultErrorMapper<Error, ProblemHttpResult>
{
    public ProblemHttpResult Map(Error error)
    {
        return error.ToProblemDetails();
    }
}