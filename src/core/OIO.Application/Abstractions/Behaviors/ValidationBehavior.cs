using CSharpFunctionalExtensions;
using MediatR;
using OIO.Application.Abstractions.Messaging;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Abstractions.Behaviors;

public sealed class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
    where TResponse : IUnitResult<Error>
{

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (request is not IHasValidate validator)
            return await next(cancellationToken);
        
        var errorResult = validator.Validate();

        if (!errorResult.HasErrors) 
            return await next(cancellationToken);
        
        // UnitResult<Error>
        if (typeof(TResponse) == typeof(UnitResult<Error>))
            return (TResponse)(object)UnitResult.Failure<Error>(errorResult);

        // Result<T, Error>
        if (!typeof(TResponse).IsGenericType ||
            typeof(TResponse).GetGenericTypeDefinition() != typeof(Result<,>))
            throw new InvalidOperationException($"Unsupported response type: {typeof(TResponse).FullName}");
        
        var t = typeof(TResponse).GetGenericArguments()[0]; // T in Result<T, Error>

        var failureMethod = typeof(Result)
            .GetMethods()
            .Single(m => m is { Name: nameof(Result.Failure), IsGenericMethodDefinition: true }
                         && m.GetGenericArguments().Length == 2);

        var generic = failureMethod.MakeGenericMethod(t, typeof(Error));
        var resultObj = generic.Invoke(null, [(Error)errorResult])!;

        return (TResponse)resultObj;

    }
}
