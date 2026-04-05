using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using OIO.Domain.SeedWork.Errors;
using OIO.Domain.SeedWork.Exceptions;

namespace OIO.Api.Middleware;

internal sealed class GlobalExceptionHandler
    : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;
    private readonly IProblemDetailsService _problemDetailsService;
    private readonly IWebHostEnvironment _env;
    
    public GlobalExceptionHandler(
        ILogger<GlobalExceptionHandler> logger,
        IProblemDetailsService problemDetailsService,
        IWebHostEnvironment env)
    {
        _logger = logger;
        _problemDetailsService = problemDetailsService;
        _env = env;
    }
    
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        _logger.LogError(exception, "Unhandled exception occurred");

        if (exception is DomainException domainException)
        {
            var error = domainException.Error;
            var problemDetails = new ProblemDetails()
            {
                Title = error.Message,
                Detail = error.Code,
                Status = error.GetStatus(),
            };

            return await _problemDetailsService.TryWriteAsync(new ProblemDetailsContext
            {
                HttpContext = httpContext,
                ProblemDetails = problemDetails
            });
        }

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
        
        var problemDetailsContext = new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails =
            {
                Title = "Unexpected.Error",
                Detail = _env.IsDevelopment() ? exception.Message : "An unexpected error occurred.",
                Status = StatusCodes.Status500InternalServerError,
            }
        };
        
        return await _problemDetailsService.TryWriteAsync(problemDetailsContext);
    }
}