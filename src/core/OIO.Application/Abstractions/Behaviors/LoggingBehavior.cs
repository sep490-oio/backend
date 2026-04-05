using System.Diagnostics;
using CSharpFunctionalExtensions;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OIO.Application.Abstractions.Commons;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.UserContext.Services;
using OIO.Domain.SeedWork.Errors;
using Serilog.Context;

namespace OIO.Application.Abstractions.Behaviors;

public sealed class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;
    private readonly ICurrentUser _currentUser;
    private readonly IOptionsMonitor<AppLoggingOptions> _loggingOptions;

    public LoggingBehavior(
        ILogger<LoggingBehavior<TRequest, TResponse>> logger,
        ICurrentUser currentUser,
        IOptionsMonitor<AppLoggingOptions> loggingOptions)
    {
        _logger = logger;
        _currentUser = currentUser;
        _loggingOptions = loggingOptions;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var requestKind = request is IBaseCommand ? "Command" : "Query";
        var userId = _currentUser.IsAuthenticated ? _currentUser.UserId.Value : (Guid?)null;
        var stopwatch = Stopwatch.StartNew();

        using (LogContext.PushProperty("RequestName", requestName))
        using (LogContext.PushProperty("RequestKind", requestKind))
        using (LogContext.PushProperty("UserId", userId))
        {
            try
            {
                var response = await next();
                stopwatch.Stop();

                var inspection = ResponseInspection.Inspect(response);
                var level = GetCompletionLevel(
                    requestKind,
                    inspection.IsFailure,
                    stopwatch.ElapsedMilliseconds,
                    _loggingOptions.CurrentValue.MediatR);

                LogCompletion(level, requestName, requestKind, inspection.Outcome, stopwatch.ElapsedMilliseconds, inspection.ErrorCode);
                return response;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(
                    ex,
                    "MediatR Failed {RequestKind} {RequestName} in {DurationMs}ms",
                    requestKind,
                    requestName,
                    stopwatch.ElapsedMilliseconds);
                throw;
            }
        }
    }

    private static LogLevel GetCompletionLevel(
        string requestKind,
        bool isFailure,
        long durationMs,
        AppMediatRLoggingOptions options)
    {
        if (isFailure || durationMs >= options.SlowRequestThresholdMs)
        {
            return LogLevel.Warning;
        }

        if (requestKind == "Command")
        {
            return LogLevel.Information;
        }

        return options.LogSuccessfulQueriesAtDebug
            ? LogLevel.Debug
            : LogLevel.Information;
    }

    private void LogCompletion(
        LogLevel level,
        string requestName,
        string requestKind,
        string outcome,
        long durationMs,
        string? errorCode)
    {
        if (level == LogLevel.Debug)
        {
            _logger.LogDebug(
                "MediatR {Outcome} {RequestKind} {RequestName} in {DurationMs}ms {ErrorCode}",
                outcome,
                requestKind,
                requestName,
                durationMs,
                errorCode);
            return;
        }

        if (level == LogLevel.Warning)
        {
            _logger.LogWarning(
                "MediatR {Outcome} {RequestKind} {RequestName} in {DurationMs}ms {ErrorCode}",
                outcome,
                requestKind,
                requestName,
                durationMs,
                errorCode);
            return;
        }

        _logger.LogInformation(
            "MediatR {Outcome} {RequestKind} {RequestName} in {DurationMs}ms {ErrorCode}",
            outcome,
            requestKind,
            requestName,
            durationMs,
            errorCode);
    }

    private sealed record ResponseInspection(bool IsFailure, string Outcome, string? ErrorCode)
    {
        public static ResponseInspection Inspect(object? response)
        {
            return response switch
            {
                UnitResult<Error> unitResult when unitResult.IsFailure => new(true, "Failed", unitResult.Error.Code),
                _ => InspectByReflection(response)
            };
        }

        private static ResponseInspection InspectByReflection(object? response)
        {
            if (response is null)
            {
                return new ResponseInspection(false, "Succeeded", null);
            }

            var responseType = response.GetType();
            var isFailureProperty = responseType.GetProperty("IsFailure");

            if (isFailureProperty?.PropertyType != typeof(bool))
            {
                return new ResponseInspection(false, "Succeeded", null);
            }

            var isFailure = (bool)(isFailureProperty.GetValue(response) ?? false);
            if (!isFailure)
            {
                return new ResponseInspection(false, "Succeeded", null);
            }

            var errorCode = responseType
                .GetProperty("Error")
                ?.GetValue(response)?
                .GetType()
                .GetProperty("Code")
                ?.GetValue(responseType.GetProperty("Error")?.GetValue(response))
                ?.ToString();

            return new ResponseInspection(true, "Failed", errorCode);
        }
    }
}
