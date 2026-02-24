using System.Reflection;
using CSharpFunctionalExtensions;
using FluentValidation;
using MediatR;
using OIO.Application.Abstractions.Messaging;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Abstractions.Behaviors;

// public sealed class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
//     where TRequest : IRequest<TResponse>
//     where TResponse : IResult, IError<Error>
// {
//     private readonly IEnumerable<IValidator<TRequest>> _validators;
//
//     public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
//     {
//         _validators = validators;
//     }
//     
//     private readonly MethodInfo _resultFailureMethod =
//         typeof(Result)
//             .GetMethods(BindingFlags.Public | BindingFlags.Static)
//             .Single(m =>
//                 m is { Name: nameof(Result.Failure), IsGenericMethodDefinition: true }
//                 && m.GetGenericArguments().Length == 2
//                 && m.GetParameters().Length == 1);
//
//     public async Task<TResponse> Handle(
//         TRequest request,
//         RequestHandlerDelegate<TResponse> next,
//         CancellationToken cancellationToken)
//     {
//         var validators = _validators as IValidator<TRequest>[] ?? _validators.ToArray();
//         
//         if (validators.Length == 0)
//             return await next(cancellationToken);
//
//         var context = new ValidationContext<TRequest>(request);
//
//         var validationResults = await Task.WhenAll(
//             _validators.Select(v => v.ValidateAsync(context, cancellationToken)));
//
//         var failures = validationResults
//             .SelectMany(r => r.Errors)
//             .Where(e => e is not null)
//             .Select(e => new ValidationError(e!.PropertyName, e.ErrorCode, e.ErrorMessage))
//             .Distinct()
//             .ToArray();
//
//
//         if (failures.Length == 0)
//             return await next(cancellationToken);
//         
//         var ve = new ValidationErrors(prefix: nameof(TRequest), items: failures);
//         
//         return CreateFailureResponse(ve);
//     }
//     
//     private TResponse CreateFailureResponse(Error error)
//     {
//         if (typeof(TResponse) == typeof(UnitResult<Error>))
//             return (TResponse)(object)UnitResult.Failure(error);
//
//         var t = typeof(TResponse);
//         
//         if (!t.IsGenericType || t.GetGenericTypeDefinition() != typeof(Result<,>))
//             throw new InvalidOperationException(
//                 $"Unsupported response type: {t.FullName}. Expected Result<T, Error> or UnitResult<Error>.");
//         
//         var args = t.GetGenericArguments(); // [TValue, TError]
//         
//         if (args[1] != typeof(Error))
//         {
//             throw new InvalidOperationException(
//                 $"ValidationBehavior only supports Result<TValue, Error> but got Result<{args[0].Name}, {args[1].Name}>");
//         }
//
//         var mi = _resultFailureMethod.MakeGenericMethod(args[0], typeof(Error));
//         
//         return (TResponse)mi.Invoke(null, [error])!;
//
//     }
// }

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
            return (TResponse)(object)UnitResult.Failure(errorResult);

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
        var resultObj = generic.Invoke(null, [errorResult])!;

        return (TResponse)resultObj;

    }
}