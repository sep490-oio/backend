using Microsoft.AspNetCore.Diagnostics;

namespace OIO.Api.Middleware;

internal sealed class GlobalExceptionHandler
    : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;
    private readonly IProblemDetailsService _problemDetailsService;
    
    public GlobalExceptionHandler(
        ILogger<GlobalExceptionHandler> logger,
        IProblemDetailsService problemDetailsService)
    {
        _logger = logger;
        _problemDetailsService = problemDetailsService;
    }
    
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        _logger.LogError(exception, "Unhandled exception occurred");

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
        
        var problemDetailsContext = new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails =
            {
                Title = "Unexpected.Error",
                Detail = "An unexpected error occurred.",
                Status = StatusCodes.Status500InternalServerError,
            }
        };
        
        return await _problemDetailsService.TryWriteAsync(problemDetailsContext);
    }
}
