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