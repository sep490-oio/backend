using CSharpFunctionalExtensions.HttpResults;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using OIO.Domain.SeedWork.Checks;

namespace OIO.Domain.SeedWork.Errors;

public class ErrorMapper : IResultErrorMapper<Error, ProblemHttpResult>
{
    public ProblemHttpResult Map(Error error)
    {
        var status = error.GetStatus();
        var (title, type) = ProblemDetailsMappingProvider.FindMapping(status);
        var problemDetails = new ProblemDetails()
        {
            Status = status,
            Title = title,
            Type = type,
            Detail = error.Message,
            Extensions = new Dictionary<string, object?>()
            {
                ["code"] = error.Code
            }
        };

        if (error is not ViolationsError validationError) 
            return TypedResults.Problem(problemDetails);
        
        var errorsDict = validationError.Violations.GroupBy(e => ((ICheckError)e).PropertyName)
            .ToDictionary(g => g.Key, 
                g => 
                    g.Select(e => new {e.Message, e.Code}).ToArray());
        
        problemDetails.Extensions["errors"] = errorsDict;

        return TypedResults.Problem(problemDetails);  
    }
}