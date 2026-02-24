using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using CSharpFunctionalExtensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Domain.SeedWork.Checks.Extensions;

public static class CheckFieldExtensions
{
    extension<TOwner>(TOwner)
    {
        public static CheckContext<TOwner> Check(
            string mode = CheckMode.CollectAll,
            bool isInvariant = false,
            string? separator = null)
            => new(new CheckState(mode), isInvariant, separator);

        public static CheckField<TOwner, TProp> Field<TProp>(
            TProp value,
            string propertyName,
            string? fieldName = null,
            string mode = CheckMode.CollectAll,
            bool isInvariant = false,
            string? separator = null,
            [CallerArgumentExpression("value")] string expr = "")
            => Check<TOwner>(mode, isInvariant, separator)
                .Field(value, propertyName, fieldName, expr: expr);

        public static CheckField<TOwner, TProp> Field<TProp>(
            TProp value,
            Expression<Func<TOwner, TProp>> exprPropertyName,
            string? fieldName = null,
            string mode = CheckMode.CollectAll,
            bool isInvariant = false,
            string? separator = null)
            => Check<TOwner>(mode, isInvariant, separator).Field(value, exprPropertyName, fieldName);
        
    }
    
    extension<TOwner>(TOwner type)
    {
        public CheckContext<TOwner> Check(
            bool isInvariant = false,
            string mode = CheckMode.CollectAll,
            string? separator = null)
            => new(new CheckState(mode), isInvariant, separator);
    }

    extension<TOwner, TProperty>(CheckField<TOwner, TProperty> field)
    {
        
        public Result<TProperty, ViolationsError> ToResult(
            string? prefix = null,
            string? separator = null,
            string? suffix = null,
            string? message = null)
        {
            return field.IsFailure
                ? field.State.ToViolationsError(prefix ?? field.OwnerName, separator ?? field.Separator, suffix, message)
                : Result.Success<TProperty, ViolationsError>(field.Property);
        }
    
        public UnitResult<ViolationsError> ToUnitResult(
            string? prefix = null,
            string? separator = null,
            string? suffix = null,
            string? message = null)
        {
            return field.State.IsFailure
                ? field.State.ToViolationsError(prefix ?? field.OwnerName, separator ?? field.Separator, suffix, message)
                : UnitResult.Success<ViolationsError>();
        }
    }
}