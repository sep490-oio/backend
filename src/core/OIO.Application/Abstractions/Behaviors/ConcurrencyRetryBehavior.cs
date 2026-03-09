using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace OIO.Application.Abstractions.Behaviors;

public sealed class ConcurrencyRetryBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger<ConcurrencyRetryBehavior<TRequest, TResponse>> _logger;
    private const int MaxRetries = 3;

    public ConcurrencyRetryBehavior(
        ILogger<ConcurrencyRetryBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        for (var attempt = 1; attempt <= MaxRetries; attempt++)
        {
            try
            {
                return await next();
            }
            catch (DbUpdateConcurrencyException ex)
            {
                if (attempt == MaxRetries)
                {
                    _logger.LogError(ex,
                        "Concurrency conflict on {Request} after {MaxRetries} retries.",
                        typeof(TRequest).Name, MaxRetries);
                    throw;
                }

                _logger.LogWarning(
                    "Concurrency conflict on {Request}. Retry {Attempt}/{MaxRetries}.",
                    typeof(TRequest).Name, attempt, MaxRetries);

                // Small jitter to reduce collision
                await Task.Delay(
                    Random.Shared.Next(10, 50) * attempt,
                    cancellationToken);
            }
        }

        // Unreachable
        throw new InvalidOperationException();
    }
}