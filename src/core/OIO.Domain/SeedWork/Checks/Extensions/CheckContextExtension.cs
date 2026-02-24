using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using CSharpFunctionalExtensions;
using OIO.Domain.SeedWork.Errors;
using OIO.Domain.SeedWork.Utils;

namespace OIO.Domain.SeedWork.Checks.Extensions;

public static class CheckContextExtension
{
    extension<TOwner>(CheckContext<TOwner> context)
    {
        public CheckField<TOwner, TProperty> Field<TProperty>(
            TProperty property,
            string? propertyName = null,
            string? fieldName = null,
            string? separator = null,
            [CallerArgumentExpression("property")] string expr = "")
        {
            propertyName ??= expr.ExtractLastMember();
            return new CheckField<TOwner, TProperty>(
                property: property,
                state: context.State,
                isInvariant: context.IsInvariant,
                ownerName: context.OwnerName,
                propertyName: propertyName.ToTitleCase(),
                fieldName: (fieldName?.ToTitleCase() ?? propertyName.ToTitleCase()),
                separator: separator ?? context.Separator,
                fieldFailed: false
            );
        }

        public CheckField<TOwner, TProperty> Field<TProperty>(
            TProperty property,
            Expression<Func<TOwner, TProperty>> exprPropertyName,
            string? propertyName = null,
            string? fieldName = null,
            string? separator = null)
        {
            propertyName ??= exprPropertyName.GetOrAddName();
            return new CheckField<TOwner, TProperty>(
                property: property,
                state: context.State,
                isInvariant: context.IsInvariant,
                ownerName: context.OwnerName,
                propertyName: propertyName.ToTitleCase(),
                fieldName: (fieldName?.ToTitleCase() ?? propertyName.ToTitleCase()),
                separator: separator ?? context.Separator,
                fieldFailed: false
            );
        }
        
        public UnitResult<ViolationsError> ToUnitResult(
            string? prefix = null,
            string? separator = null,
            string? suffix = null,
            string? message = null)
        {
            return context.State.IsFailure
                ? context.State.ToViolationsError(prefix ?? context.OwnerName, separator ?? context.Separator, suffix, message)
                : UnitResult.Success<ViolationsError>();
        }
    }
}