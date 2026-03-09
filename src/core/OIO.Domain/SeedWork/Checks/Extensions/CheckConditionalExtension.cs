using CSharpFunctionalExtensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Domain.SeedWork.Checks.Extensions;

public static class CheckConditionalExtensions
{
    extension<TOwner, TProp>(CheckField<TOwner, TProp> check)
    {
        public CheckField<TOwner, TProp> When(bool condition,
            Func<CheckField<TOwner, TProp>, CheckField<TOwner, TProp>> then)
            => condition ? then(check) : check;

        public CheckField<TOwner, TProp> Unless(bool condition,
            Func<CheckField<TOwner, TProp>, CheckField<TOwner, TProp>> then)
            => condition ? check : then(check);

        public CheckField<TOwner, TProp> IfPassed(Func<CheckField<TOwner, TProp>, CheckField<TOwner, TProp>> then)
            => check is { IsFailure: false, ShouldContinue: true } ? then(check) : check;

        public CheckField<TOwner, TProp> IfFailed(Func<CheckField<TOwner, TProp>, CheckField<TOwner, TProp>> then)
            => check.IsFailure ? then(check) : check;
        
        public CheckField<TOwner, TProp> Must<TResult>(
             Func<TProp, TResult> must,
             string? message = null,
             Error? error = null) where TResult : IUnitResult<Error>   
        {
            var result = must(check.Property);
            
            if (!check.ShouldContinue || result.IsSuccess)
                return check;

            var err = error ?? result.Error;

            return check.Fail(err);
        }
    }

    extension<TOwner, TProp>(CheckField<TOwner, TProp?> check) where TProp : struct
    {
        public CheckField<TOwner, TProp?> WhenHasValue(
            Func<CheckField<TOwner, TProp>, CheckField<TOwner, TProp>> then)
        {
            if (!check.ShouldContinue || !check.Property.HasValue)
                return check;

            var narrowed = check.Narrow(check.Property.Value);
            var after = then(narrowed);

            return after.Narrow<TProp?>(after.Property);
        }
    }

    extension<TOwner, TProp>(CheckField<TOwner, TProp?> check) where TProp : class?
    {
        public CheckField<TOwner, TProp?> WhenHasValue(
            Func<CheckField<TOwner, TProp>, CheckField<TOwner, TProp>> then)
        {
            if (!check.ShouldContinue || check.Property is null)
                return check;

            var narrowed = check.Narrow(check.Property!);
            var after = then(narrowed);

            return after.Narrow<TProp?>(after.Property);
        }
    }

}