using System.Collections;
using System.Linq.Expressions;
using System.Numerics;
using System.Runtime.CompilerServices;
using CSharpFunctionalExtensions.HttpResults;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using OIO.Domain.SeedWork.Checks;
using OIO.Domain.SeedWork.Errors.ErrorCatalogs;
using OIO.Domain.SeedWork.Utils;

namespace OIO.Domain.SeedWork.Errors;

public static class ErrorExtensions
{
    extension(Error error)
    {
        #region To problem details

        public ProblemHttpResult ToProblemDetails()
        {
            var status = error.GetStatus();
            var (title, type) = ProblemDetailsMappingProvider.FindMapping(status);
            var problemDetails = new ProblemDetails()
            {
                Status = status,
                Title = title,
                Type = type,
                Detail = error.Message,
                Extensions = new Dictionary<string, object?>()
                {
                    ["code"] = error.Code
                }
            };

            if (error is not ViolationsError violationsError) 
                return TypedResults.Problem(problemDetails);
        
            var errorsDict = violationsError.Violations.GroupBy(e => ((ICheckError)e).PropertyName)
                .ToDictionary(g => g.Key, 
                    g => 
                        g.Select(e => e.Message).ToArray());
        
            problemDetails.Extensions["errors"] = errorsDict;

            return TypedResults.Problem(problemDetails);  
        }

        #endregion
        #region Required

        public static Error NotNull<TProperty>(
            TProperty property,
            bool isInvariant = false,
            string? codePrefix = null,
            string propertyName = "",
            string? field = null, 
            string? message = null,
            [CallerArgumentExpression("property")] string expr = "")
        {
            var (fieldDisplay, code) = ResolveFieldAndCode(codePrefix, propertyName, field, expr);
            return ErrorCatalog.GeneralError.Required.NotNull(fieldDisplay, code, isInvariant, message);
        }
        
        public static Error NotNull<TOwner, TProperty>(
            Expression<Func<TOwner, TProperty>> exprPropertyName,
            bool isInvariant = false,
            string? codePrefix = null,
            string? propertyName = null,
            string? field = null, 
            string? message = null)
        {
            var (fieldDisplay, code) = ResolveFieldAndCode(exprPropertyName, codePrefix, propertyName, field);
            return ErrorCatalog.GeneralError.Required.NotNull(fieldDisplay, code, isInvariant, message);
        }
        
        public static Error NotEmpty<TProperty>(
            TProperty property,
            bool isInvariant = false,
            string? codePrefix = null,
            string propertyName = "",
            string? field = null, 
            string? message = null,
            [CallerArgumentExpression("property")] string expr = "")
        {
            var (fieldDisplay, code) = ResolveFieldAndCode(codePrefix, propertyName, field, expr);
            return ErrorCatalog.GeneralError.Required.NotEmpty(fieldDisplay, code, isInvariant, message);
        }
        
        public static Error NotEmpty<TOwner, TProperty>(
            Expression<Func<TOwner, TProperty>> exprPropertyName,
            bool isInvariant = false,
            string? codePrefix = null,
            string? propertyName = null,
            string? field = null, 
            string? message = null)
        {
            var (fieldDisplay, code) = ResolveFieldAndCode(exprPropertyName, codePrefix, propertyName, field);
            return ErrorCatalog.GeneralError.Required.NotEmpty(fieldDisplay, code, isInvariant, message);
        }
        
        public static Error NotEmpty(
            Guid property,
            bool isInvariant = false,
            string? codePrefix = null,
            string propertyName = "",
            string? field = null, 
            string? message = null,
            [CallerArgumentExpression("property")] string expr = "")
        {
            var (fieldDisplay, code) = ResolveFieldAndCode(codePrefix, propertyName, field, expr);
            return ErrorCatalog.GeneralError.Required.NotEmptyGuid(fieldDisplay, code, isInvariant, message);
        }
        
        public static Error NotEmpty<TOwner>(
            Expression<Func<TOwner, Guid>> exprPropertyName,
            bool isInvariant = false,
            string? codePrefix = null,
            string? propertyName = null,
            string? field = null, 
            string? message = null)
        {
            var (fieldDisplay, code) = ResolveFieldAndCode(exprPropertyName, codePrefix, propertyName, field);
            return ErrorCatalog.GeneralError.Required.NotEmptyGuid(fieldDisplay, code, isInvariant, message);
        }
        
        public static Error NotDefault<TProperty>(
            TProperty? property,
            TProperty? @default,
            bool isInvariant = false,
            string? codePrefix = null,
            string propertyName = "",
            string? field = null, 
            string? message = null,
            [CallerArgumentExpression("property")] string expr = "") where TProperty  : struct
        {
            var (fieldDisplay, code) = ResolveFieldAndCode(codePrefix, propertyName, field, expr);
            return ErrorCatalog.GeneralError.Required.NotDefault(fieldDisplay, @default, code, isInvariant, message);
        }
        
        public static Error NotDefault<TProperty>(
            TProperty property,
            TProperty? @default,
            bool isInvariant = false,
            string? codePrefix = null,
            string propertyName = "",
            string? field = null, 
            string? message = null,
            [CallerArgumentExpression("property")] string expr = "") where TProperty : struct
        {
            var (fieldDisplay, code) = ResolveFieldAndCode(codePrefix, propertyName, field, expr);
            return ErrorCatalog.GeneralError.Required.NotDefault(fieldDisplay, @default, code, isInvariant, message);
        }
        
        public static Error NotDefault<TProperty>(
            TProperty? property,
            TProperty? @default,
            bool isInvariant = false,
            string? codePrefix = null,
            string propertyName = "",
            string? field = null, 
            string? message = null,
            [CallerArgumentExpression("property")] string expr = "")
        {
            var (fieldDisplay, code) = ResolveFieldAndCode(codePrefix, propertyName, field, expr);
            return ErrorCatalog.GeneralError.Required.NotDefault(fieldDisplay, @default, code, isInvariant, message);
        }
        
        public static Error NotDefault<TOwner, TProperty>(
            Expression<Func<TOwner, TProperty>> exprPropertyName,
            TProperty? @default,
            bool isInvariant = false,
            string? codePrefix = null,
            string? propertyName = null,
            string? field = null, 
            string? message = null)
        {
            var (fieldDisplay, code) = ResolveFieldAndCode(exprPropertyName, codePrefix, propertyName, field);
            return ErrorCatalog.GeneralError.Required.NotDefault(fieldDisplay, @default, code, isInvariant, message);
        }
        
        public static Error NotBlank<TProperty>(
            TProperty property,
            bool isInvariant = false,
            string? codePrefix = null,
            string propertyName = "",
            string? field = null, 
            string? message = null,
            [CallerArgumentExpression("property")] string expr = "")
        {
            var (fieldDisplay, code) = ResolveFieldAndCode(codePrefix, propertyName, field, expr);
            return ErrorCatalog.GeneralError.Required.NotBlank(fieldDisplay, code, isInvariant, message);
        }
        
        public static Error NotBlank<TOwner, TProperty>(
            Expression<Func<TOwner, TProperty>> exprPropertyName,
            bool isInvariant = false,
            string? codePrefix = null,
            string? propertyName = null,
            string? field = null, 
            string? message = null)
        {
            var (fieldDisplay, code) = ResolveFieldAndCode(exprPropertyName, codePrefix, propertyName, field);
            return ErrorCatalog.GeneralError.Required.NotBlank(fieldDisplay, code, isInvariant, message);
        }

        #endregion

        #region Text

        public static Error Matches(
            string property,
            string pattern,
            bool isInvariant = false,
            string? codePrefix = null,
            string propertyName = "",
            string? field = null, 
            string? message = null,
            [CallerArgumentExpression("property")] string expr = "")
        {
            var (fieldDisplay, code) = ResolveFieldAndCode(codePrefix, propertyName, field, expr);
            return ErrorCatalog.GeneralError.Text.Matches(fieldDisplay, pattern, code, isInvariant, message);
        }
        
        public static Error Matches<TOwner, TProperty>(
            Expression<Func<TOwner, TProperty>> exprPropertyName,
            string pattern,
            bool isInvariant = false,
            string? codePrefix = null,
            string? propertyName = null,
            string? field = null, 
            string? message = null)
        {
            var (fieldDisplay, code) = ResolveFieldAndCode(exprPropertyName, codePrefix, propertyName, field);
            return ErrorCatalog.GeneralError.Text.Matches(fieldDisplay, pattern, code, isInvariant, message);
        }
        
        public static Error StartsWith(
            string property,
            string prefix,
            bool isInvariant = false,
            string? codePrefix = null,
            string propertyName = "",
            string? field = null, 
            string? message = null,
            [CallerArgumentExpression("property")] string expr = "")
        {
            var (fieldDisplay, code) = ResolveFieldAndCode(codePrefix, propertyName, field, expr);
            return ErrorCatalog.GeneralError.Text.StartsWith(fieldDisplay, prefix, code, isInvariant, message);
        }
        
        public static Error StartsWith<TOwner, TProperty>(
            Expression<Func<TOwner, TProperty>> exprPropertyName,
            string prefix,
            bool isInvariant = false,
            string? codePrefix = null,
            string? propertyName = null,
            string? field = null, 
            string? message = null)
        {
            var (fieldDisplay, code) = ResolveFieldAndCode(exprPropertyName, codePrefix, propertyName, field);
            return ErrorCatalog.GeneralError.Text.StartsWith(fieldDisplay, prefix, code, isInvariant, message);
        }
        
        public static Error EndsWith(
            string property,
            string suffix,
            bool isInvariant = false,
            string? codePrefix = null,
            string propertyName = "",
            string? field = null, 
            string? message = null,
            [CallerArgumentExpression("property")] string expr = "")
        {
            var (fieldDisplay, code) = ResolveFieldAndCode(codePrefix, propertyName, field, expr);
            return ErrorCatalog.GeneralError.Text.EndsWith(fieldDisplay, suffix, code, isInvariant, message);
        }
        
        public static Error EndsWith<TOwner, TProperty>(
            Expression<Func<TOwner, TProperty>> exprPropertyName,
            string suffix,
            bool isInvariant = false,
            string? codePrefix = null,
            string? propertyName = null,
            string? field = null, 
            string? message = null)
        {
            var (fieldDisplay, code) = ResolveFieldAndCode(exprPropertyName, codePrefix, propertyName, field);
            return ErrorCatalog.GeneralError.Text.EndsWith(fieldDisplay, suffix, code, isInvariant, message);
        }
        
        public static Error Contains(
            string property,
            string substring,
            bool isInvariant = false,
            string? codePrefix = null,
            string propertyName = "",
            string? field = null, 
            string? message = null,
            [CallerArgumentExpression("property")] string expr = "")
        {
            var (fieldDisplay, code) = ResolveFieldAndCode(codePrefix, propertyName, field, expr);
            return ErrorCatalog.GeneralError.Text.Contains(fieldDisplay, substring, code, isInvariant, message);
        }
        
        public static Error Contains<TOwner, TProperty>(
            Expression<Func<TOwner, TProperty>> exprPropertyName,
            string substring,
            bool isInvariant = false,
            string? codePrefix = null,
            string? propertyName = null,
            string? field = null, 
            string? message = null)
        {
            var (fieldDisplay, code) = ResolveFieldAndCode(exprPropertyName, codePrefix, propertyName, field);
            return ErrorCatalog.GeneralError.Text.Contains(fieldDisplay, substring, code, isInvariant, message);
        }
        
        public static Error MaxLength(
            string property,
            int max,
            bool isInvariant = false,
            string? codePrefix = null,
            string propertyName = "",
            string? field = null, 
            string? message = null,
            [CallerArgumentExpression("property")] string expr = "")
        {
            var (fieldDisplay, code) = ResolveFieldAndCode(codePrefix, propertyName, field, expr);
            return ErrorCatalog.GeneralError.Text.MaxLength(fieldDisplay, max, code, isInvariant, message);
        }
        
        public static Error MaxLength<TOwner, TProperty>(
            Expression<Func<TOwner, TProperty>> exprPropertyName,
            int max,
            bool isInvariant = false,
            string? codePrefix = null,
            string? propertyName = null,
            string? field = null, 
            string? message = null)
        {
            var (fieldDisplay, code) = ResolveFieldAndCode(exprPropertyName, codePrefix, propertyName, field);
            return ErrorCatalog.GeneralError.Text.MaxLength(fieldDisplay, max, code, isInvariant, message);
        }
        
        public static Error MinLength(
            string property,
            int min,
            bool isInvariant = false,
            string? codePrefix = null,
            string propertyName = "",
            string? field = null, 
            string? message = null,
            [CallerArgumentExpression("property")] string expr = "")
        {
            var (fieldDisplay, code) = ResolveFieldAndCode(codePrefix, propertyName, field, expr);
            return ErrorCatalog.GeneralError.Text.MinLength(fieldDisplay, min, code, isInvariant, message);
        }
        
        public static Error MinLength<TOwner, TProperty>(
            Expression<Func<TOwner, TProperty>> exprPropertyName,
            int min,
            bool isInvariant = false,
            string? codePrefix = null,
            string? propertyName = null,
            string? field = null, 
            string? message = null)
        {
            var (fieldDisplay, code) = ResolveFieldAndCode(exprPropertyName, codePrefix, propertyName, field);
            return ErrorCatalog.GeneralError.Text.MinLength(fieldDisplay, min, code, isInvariant, message);
        }
        
        public static Error ExactLength(
            string property,
            int length,
            bool isInvariant = false,
            string? codePrefix = null,
            string propertyName = "",
            string? field = null, 
            string? message = null,
            [CallerArgumentExpression("property")] string expr = "")
        {
            var (fieldDisplay, code) = ResolveFieldAndCode(codePrefix, propertyName, field, expr);
            return ErrorCatalog.GeneralError.Text.ExactLength(fieldDisplay, length, code, isInvariant, message);
        }
        
        public static Error ExactLength<TOwner, TProperty>(
            Expression<Func<TOwner, TProperty>> exprPropertyName,
            int length,
            bool isInvariant = false,
            string? codePrefix = null,
            string? propertyName = null,
            string? field = null, 
            string? message = null)
        {
            var (fieldDisplay, code) = ResolveFieldAndCode(exprPropertyName, codePrefix, propertyName, field);
            return ErrorCatalog.GeneralError.Text.ExactLength(fieldDisplay, length, code, isInvariant, message);
        }

        public static Error LengthBetween(
            string property,
            int min,
            int max,
            bool isInvariant = false,
            string? codePrefix = null,
            string propertyName = "",
            string? field = null, 
            string? message = null,
            [CallerArgumentExpression("property")] string expr = "")
        {
            var (fieldDisplay, code) = ResolveFieldAndCode(codePrefix, propertyName, field, expr);
            return ErrorCatalog.GeneralError.Text.LengthBetween(fieldDisplay, min, max, code, isInvariant, message);
        }
        
        public static Error LengthBetween<TOwner, TProperty>(
            Expression<Func<TOwner, TProperty>> exprPropertyName,
            int min,
            int max,
            bool isInvariant = false,
            string? codePrefix = null,
            string? propertyName = null,
            string? field = null, 
            string? message = null)
        {
            var (fieldDisplay, code) = ResolveFieldAndCode(exprPropertyName, codePrefix, propertyName, field);
            return ErrorCatalog.GeneralError.Text.LengthBetween(fieldDisplay, min, max, code, isInvariant, message);
        }
        
        public static Error Format(
            string property,
            bool isInvariant = false,
            string? codePrefix = null,
            string propertyName = "",
            string? field = null, 
            string? message = null,
            [CallerArgumentExpression("property")] string expr = "")
        {
            var (fieldDisplay, code) = ResolveFieldAndCode(codePrefix, propertyName, field, expr);
            return ErrorCatalog.GeneralError.Text.Format(fieldDisplay, code, isInvariant, message);
        }
        
        public static Error Format<TOwner, TProperty>(
            Expression<Func<TOwner, TProperty>> exprPropertyName,
            bool isInvariant = false,
            string? codePrefix = null,
            string? propertyName = null,
            string? field = null, 
            string? message = null)
        {
            var (fieldDisplay, code) = ResolveFieldAndCode(exprPropertyName, codePrefix, propertyName, field);
            return ErrorCatalog.GeneralError.Text.Format(fieldDisplay, code, isInvariant, message);
        }
        #endregion

        #region Numberic

        public static Error Positive<TProperty>(
            TProperty property,
            bool isInvariant = false,
            string? codePrefix = null,
            string propertyName = "",
            string? field = null, 
            string? message = null,
            [CallerArgumentExpression("property")] string expr = "") where  TProperty : INumber<TProperty>
        {
            var (fieldDisplay, code) = ResolveFieldAndCode(codePrefix, propertyName, field, expr);
            return ErrorCatalog.GeneralError.Numeric.Positive(fieldDisplay, code, isInvariant, message);
        }
        
        public static Error Positive<TOwner, TProperty>(
            Expression<Func<TOwner, TProperty>> exprPropertyName,
            bool isInvariant = false,
            string? codePrefix = null,
            string? propertyName = null,
            string? field = null, 
            string? message = null)
        {
            var (fieldDisplay, code) = ResolveFieldAndCode(exprPropertyName, codePrefix, propertyName, field);
            return ErrorCatalog.GeneralError.Numeric.Positive(fieldDisplay, code, isInvariant, message);
        }
        
        public static Error Negative<TProperty>(
            TProperty property,
            bool isInvariant = false,
            string? codePrefix = null,
            string propertyName = "",
            string? field = null, 
            string? message = null,
            [CallerArgumentExpression("property")] string expr = "") where  TProperty : INumber<TProperty>
        {
            var (fieldDisplay, code) = ResolveFieldAndCode(codePrefix, propertyName, field, expr);
            return ErrorCatalog.GeneralError.Numeric.Negative(fieldDisplay, code, isInvariant, message);
        }
        
        public static Error Negative<TOwner, TProperty>(
            Expression<Func<TOwner, TProperty>> exprPropertyName,
            bool isInvariant = false,
            string? codePrefix = null,
            string? propertyName = null,
            string? field = null, 
            string? message = null)
        {
            var (fieldDisplay, code) = ResolveFieldAndCode(exprPropertyName, codePrefix, propertyName, field);
            return ErrorCatalog.GeneralError.Numeric.Negative(fieldDisplay, code, isInvariant, message);
        }
        
        public static Error NonPositive<TProperty>(
            TProperty property,
            bool isInvariant = false,
            string? codePrefix = null,
            string propertyName = "",
            string? field = null, 
            string? message = null,
            [CallerArgumentExpression("property")] string expr = "") where  TProperty : INumber<TProperty>
        {
            var (fieldDisplay, code) = ResolveFieldAndCode(codePrefix, propertyName, field, expr);
            return ErrorCatalog.GeneralError.Numeric.NonPositive(fieldDisplay, code, isInvariant, message);
        }
        
        public static Error NonPositive<TOwner, TProperty>(
            Expression<Func<TOwner, TProperty>> exprPropertyName,
            bool isInvariant = false,
            string? codePrefix = null,
            string? propertyName = null,
            string? field = null, 
            string? message = null)
        {
            var (fieldDisplay, code) = ResolveFieldAndCode(exprPropertyName, codePrefix, propertyName, field);
            return ErrorCatalog.GeneralError.Numeric.NonPositive(fieldDisplay, code, isInvariant, message);
        }
        
        public static Error NonNegative<TProperty>(
            TProperty property,
            bool isInvariant = false,
            string? codePrefix = null,
            string propertyName = "",
            string? field = null, 
            string? message = null,
            [CallerArgumentExpression("property")] string expr = "") where  TProperty : INumber<TProperty>
        {
            var (fieldDisplay, code) = ResolveFieldAndCode(codePrefix, propertyName, field, expr);
            return ErrorCatalog.GeneralError.Numeric.NonNegative(fieldDisplay, code, isInvariant, message);
        }

        public static Error NonNegative<TOwner, TProperty>(
            Expression<Func<TOwner, TProperty>> exprPropertyName,
            bool isInvariant = false,
            string? codePrefix = null,
            string? propertyName = null,
            string? field = null, 
            string? message = null)
        {
            var (fieldDisplay, code) = ResolveFieldAndCode(exprPropertyName, codePrefix, propertyName, field);
            return ErrorCatalog.GeneralError.Numeric.NonNegative(fieldDisplay, code, isInvariant, message);
        }
        
        public static Error Zero<TProperty>(
            TProperty property,
            bool isInvariant = false,
            string? codePrefix = null,
            string propertyName = "",
            string? field = null, 
            string? message = null,
            [CallerArgumentExpression("property")] string expr = "") where  TProperty : INumber<TProperty>
        {
            var (fieldDisplay, code) = ResolveFieldAndCode(codePrefix, propertyName, field, expr);
            return ErrorCatalog.GeneralError.Numeric.Zero(fieldDisplay, code, isInvariant, message);
        }
        
        public static Error Zero<TOwner, TProperty>(
            Expression<Func<TOwner, TProperty>> exprPropertyName,
            bool isInvariant = false,
            string? codePrefix = null,
            string? propertyName = null,
            string? field = null, 
            string? message = null)
        {
            var (fieldDisplay, code) = ResolveFieldAndCode(exprPropertyName, codePrefix, propertyName, field);
            return ErrorCatalog.GeneralError.Numeric.Zero(fieldDisplay, code, isInvariant, message);
        }
        
        public static Error NonZero<TProperty>(
            TProperty property,
            bool isInvariant = false,
            string? codePrefix = null,
            string propertyName = "",
            string? field = null, 
            string? message = null,
            [CallerArgumentExpression("property")] string expr = "") where  TProperty : INumber<TProperty>
        {
            var (fieldDisplay, code) = ResolveFieldAndCode(codePrefix, propertyName, field, expr);
            return ErrorCatalog.GeneralError.Numeric.NonZero(fieldDisplay, code, isInvariant, message);
        }
        
        public static Error NonZero<TOwner, TProperty>(
            Expression<Func<TOwner, TProperty>> exprPropertyName,
            bool isInvariant = false,
            string? codePrefix = null,
            string? propertyName = null,
            string? field = null, 
            string? message = null)
        {
            var (fieldDisplay, code) = ResolveFieldAndCode(exprPropertyName, codePrefix, propertyName, field);
            return ErrorCatalog.GeneralError.Numeric.NonZero(fieldDisplay, code, isInvariant, message);
        }
        
        public static Error MultipleOf<TProperty>(
            TProperty property,
            TProperty step,
            bool isInvariant = false,
            string? codePrefix = null,
            string propertyName = "",
            string? field = null, 
            string? message = null,
            [CallerArgumentExpression("property")] string expr = "") where TProperty : INumber<TProperty>
        {
            var (fieldDisplay, code) = ResolveFieldAndCode(codePrefix, propertyName, field, expr);
            return ErrorCatalog.GeneralError.Numeric.MultipleOf(fieldDisplay, step, code, isInvariant, message);
        }
        
        public static Error MultipleOf<TOwner, TProperty>(
            Expression<Func<TOwner, TProperty>> exprPropertyName,
            TProperty step,
            bool isInvariant = false,
            string? codePrefix = null,
            string? propertyName = null,
            string? field = null, 
            string? message = null) where TProperty : INumber<TProperty>
        {
            var (fieldDisplay, code) = ResolveFieldAndCode(exprPropertyName, codePrefix, propertyName, field);
            return ErrorCatalog.GeneralError.Numeric.MultipleOf(fieldDisplay, step, code, isInvariant, message);
        }
        
        public static Error PrecisionScale<TProperty>(
            TProperty property,
            int precision,
            int scale,
            bool isInvariant = false,
            string? codePrefix = null,
            string propertyName = "",
            string? field = null, 
            string? message = null,
            [CallerArgumentExpression("property")] string expr = "") where TProperty : INumber<TProperty>
        {
            var (fieldDisplay, code) = ResolveFieldAndCode(codePrefix, propertyName, field, expr);
            return ErrorCatalog.GeneralError.Numeric.PrecisionScale(fieldDisplay, precision, scale, code, isInvariant, message);
        }
        
        public static Error PrecisionScale<TOwner, TProperty>(
            Expression<Func<TOwner, TProperty>> exprPropertyName,
            int precision,
            int scale,
            bool isInvariant = false,
            string? codePrefix = null,
            string? propertyName = null,
            string? field = null, 
            string? message = null)
        {
            var (fieldDisplay, code) = ResolveFieldAndCode(exprPropertyName, codePrefix, propertyName, field);
            return ErrorCatalog.GeneralError.Numeric.PrecisionScale(fieldDisplay, precision, scale, code, isInvariant, message);
        }
        
        #endregion

        #region Collection

        public static Error NotContains<TProperty>(
            TProperty property,
            string item,
            bool isInvariant = false,
            string? codePrefix = null,
            string propertyName = "",
            string? field = null, 
            string? message = null,
            [CallerArgumentExpression("property")] string expr = "") where TProperty : IEnumerable?
        {
            var (fieldDisplay, code) = ResolveFieldAndCode(codePrefix, propertyName, field, expr);
            return ErrorCatalog.GeneralError.Collection.NotContains(fieldDisplay, item, code, isInvariant, message);
        }

        public static Error NotContains<TOwner, TProperty>(
            Expression<Func<TOwner, TProperty>> exprPropertyName,
            string item,
            bool isInvariant = false,
            string? codePrefix = null,
            string? propertyName = null,
            string? field = null, 
            string? message = null)
        {
            var (fieldDisplay, code) = ResolveFieldAndCode(exprPropertyName, codePrefix, propertyName, field);
            return ErrorCatalog.GeneralError.Collection.NotContains(fieldDisplay, item, code, isInvariant, message);
        }
        
        public static Error NotInSet<TProperty>(
            TProperty property,
            string setString,
            bool isInvariant = false,
            string? codePrefix = null,
            string propertyName = "",
            string? field = null, 
            string? message = null,
            [CallerArgumentExpression("property")] string expr = "") where TProperty : IEnumerable?
        {
            var (fieldDisplay, code) = ResolveFieldAndCode(codePrefix, propertyName, field, expr);
            return ErrorCatalog.GeneralError.Collection.NotInSet(fieldDisplay, setString, code, isInvariant, message);
        }

        public static Error NotInSet<TOwner, TProperty>(
            Expression<Func<TOwner, TProperty>> exprPropertyName,
            string setString,
            bool isInvariant = false,
            string? codePrefix = null,
            string? propertyName = null,
            string? field = null, 
            string? message = null)
        {
            var (fieldDisplay, code) = ResolveFieldAndCode(exprPropertyName, codePrefix, propertyName, field);
            return ErrorCatalog.GeneralError.Collection.NotInSet(fieldDisplay, setString, code, isInvariant, message);
        }
        
        public static Error InSet<TProperty>(
            TProperty property,
            string setString,
            bool isInvariant = false,
            string? codePrefix = null,
            string propertyName = "",
            string? field = null, 
            string? message = null,
            [CallerArgumentExpression("property")] string expr = "") where TProperty : IEnumerable?
        {
            var (fieldDisplay, code) = ResolveFieldAndCode(codePrefix, propertyName, field, expr);
            return ErrorCatalog.GeneralError.Collection.InSet(fieldDisplay, setString, code, isInvariant, message);
        }

        public static Error InSet<TOwner, TProperty>(
            Expression<Func<TOwner, TProperty>> exprPropertyName,
            string setString,
            bool isInvariant = false,
            string? codePrefix = null,
            string? propertyName = null,
            string? field = null, 
            string? message = null)
        {
            var (fieldDisplay, code) = ResolveFieldAndCode(exprPropertyName, codePrefix, propertyName, field);
            return ErrorCatalog.GeneralError.Collection.InSet(fieldDisplay, setString, code, isInvariant, message);
        }
        
        public static Error NoDuplicates<TProperty>(
            TProperty property,
            string duplicate,
            bool isInvariant = false,
            string? codePrefix = null,
            string propertyName = "",
            string? field = null, 
            string? message = null,
            [CallerArgumentExpression("property")] string expr = "") where TProperty : IEnumerable?
        {
            var (fieldDisplay, code) = ResolveFieldAndCode(codePrefix, propertyName, field, expr);
            return ErrorCatalog.GeneralError.Collection.NoDuplicates(fieldDisplay, duplicate, code, isInvariant, message);
        }

        public static Error NoDuplicates<TOwner, TProperty>(
            Expression<Func<TOwner, TProperty>> exprPropertyName,
            string duplicate,
            bool isInvariant = false,
            string? codePrefix = null,
            string? propertyName = null,
            string? field = null, 
            string? message = null)
        {
            var (fieldDisplay, code) = ResolveFieldAndCode(exprPropertyName, codePrefix, propertyName, field);
            return ErrorCatalog.GeneralError.Collection.NoDuplicates(fieldDisplay, duplicate, code, isInvariant, message);
        }
        
        public static Error CountMin<TProperty>(
            TProperty property,
            int min,
            bool isInvariant = false,
            string? codePrefix = null,
            string propertyName = "",
            string? field = null, 
            string? message = null,
            [CallerArgumentExpression("property")] string expr = "") where TProperty : IEnumerable
        {
            var (fieldDisplay, code) = ResolveFieldAndCode(codePrefix, propertyName, field, expr);
            return ErrorCatalog.GeneralError.Collection.CountMin(fieldDisplay, min, code, isInvariant, message);
        }

        public static Error CountMin<TOwner, TProperty>(
            Expression<Func<TOwner, TProperty>> exprPropertyName,
            int min,
            bool isInvariant = false,
            string? codePrefix = null,
            string? propertyName = null,
            string? field = null, 
            string? message = null)
        {
            var (fieldDisplay, code) = ResolveFieldAndCode(exprPropertyName, codePrefix, propertyName, field);
            return ErrorCatalog.GeneralError.Collection.CountMin(fieldDisplay, min, code, isInvariant, message);
        }
        
        public static Error CountMax<TProperty>(
            TProperty property,
            int max,
            bool isInvariant = false,
            string? codePrefix = null,
            string propertyName = "",
            string? field = null, 
            string? message = null,
            [CallerArgumentExpression("property")] string expr = "") where TProperty : IEnumerable
        {
            var (fieldDisplay, code) = ResolveFieldAndCode(codePrefix, propertyName, field, expr);
            return ErrorCatalog.GeneralError.Collection.CountMax(fieldDisplay, max, code, isInvariant, message);
        }
        
        public static Error CountMax<TOwner, TProperty>(
            Expression<Func<TOwner, TProperty>> exprPropertyName,
            int max,
            bool isInvariant = false,
            string? codePrefix = null,
            string? propertyName = null,
            string? field = null, 
            string? message = null)
        {
            var (fieldDisplay, code) = ResolveFieldAndCode(exprPropertyName, codePrefix, propertyName, field);
            return ErrorCatalog.GeneralError.Collection.CountMax(fieldDisplay, max, code, isInvariant, message);
        }

        public static Error CountBetween<TProperty>(
            TProperty property,
            int min,
            int max,
            bool isInvariant = false,
            string? codePrefix = null,
            string propertyName = "",
            string? field = null, 
            string? message = null,
            [CallerArgumentExpression("property")] string expr = "") where TProperty : IEnumerable
        {
            var (fieldDisplay, code) = ResolveFieldAndCode(codePrefix, propertyName, field, expr);
            return ErrorCatalog.GeneralError.Collection.CountBetween(fieldDisplay, min, max, code, isInvariant, message);
        }
        
        public static Error CountBetween<TOwner, TProperty>(
            Expression<Func<TOwner, TProperty>> exprPropertyName,
            int min,
            int max,
            bool isInvariant = false,
            string? codePrefix = null,
            string? propertyName = null,
            string? field = null, 
            string? message = null)
        {
            var (fieldDisplay, code) = ResolveFieldAndCode(exprPropertyName, codePrefix, propertyName, field);
            return ErrorCatalog.GeneralError.Collection.CountBetween(fieldDisplay, min, max, code, isInvariant, message);
        }

        #endregion
        
        #region Scalar

        public static Error ScalarNotInSet<TProperty>(
            TProperty property,
            string setString,
            bool isInvariant = false,
            string? codePrefix = null,
            string propertyName = "",
            string? field = null, 
            string? message = null,
            [CallerArgumentExpression("property")] string expr = "")
        {
            var (fieldDisplay, code) = ResolveFieldAndCode(codePrefix, propertyName, field, expr);
            return ErrorCatalog.GeneralError.Scalar.NotInSet(fieldDisplay, setString, code, isInvariant, message);
        }
        
        public static Error ScalarNotInSet<TOwner, TProperty>(
            Expression<Func<TOwner, TProperty>> exprPropertyName,
            string setString,
            bool isInvariant = false,
            string? codePrefix = null,
            string propertyName = "",
            string? field = null, 
            string? message = null)
        {
            var (fieldDisplay, code) = ResolveFieldAndCode(exprPropertyName, codePrefix, propertyName, field);
            return ErrorCatalog.GeneralError.Scalar.NotInSet(fieldDisplay, setString, code, isInvariant, message);
        }
    
        public static Error ScalarInSet<TProperty>(
            TProperty property,
            string setString,
            bool isInvariant = false,
            string? codePrefix = null,
            string propertyName = "",
            string? field = null, 
            string? message = null,
            [CallerArgumentExpression("property")] string expr = "")
        {
            var (fieldDisplay, code) = ResolveFieldAndCode(codePrefix, propertyName, field, expr);
            return ErrorCatalog.GeneralError.Scalar.InSet(fieldDisplay, setString, code, isInvariant, message);
        }

        public static Error ScalarInSet<TOwner, TProperty>(
            Expression<Func<TOwner, TProperty>> exprPropertyName,
            string setString,
            bool isInvariant = false,
            string? codePrefix = null,
            string propertyName = "",
            string? field = null, 
            string? message = null)
        {
            var (fieldDisplay, code) = ResolveFieldAndCode(exprPropertyName, codePrefix, propertyName, field);
            return ErrorCatalog.GeneralError.Scalar.InSet(fieldDisplay, setString, code, isInvariant, message);
        }
        
        #endregion
        
        #region Enum

        public static Error InEnum<TProperty>(
            TProperty property,
            string enumName,
            string value,
            bool isInvariant = false,
            string? codePrefix = null,
            string propertyName = "",
            string? field = null, 
            string? message = null,
            [CallerArgumentExpression("property")] string expr = "")
        {
            var (fieldDisplay, code) = ResolveFieldAndCode(codePrefix, propertyName, field, expr);
            return ErrorCatalog.GeneralError.Enum.InEnum(fieldDisplay, enumName, value, code, isInvariant, message);
        }
        
        public static Error InEnum<TOwner, TProperty>(
            Expression<Func<TOwner, TProperty>> exprPropertyName,
            string enumName,
            string value,
            bool isInvariant = false,
            string? codePrefix = null,
            string propertyName = "",
            string? field = null, 
            string? message = null)
        {
            var (fieldDisplay, code) = ResolveFieldAndCode(exprPropertyName, codePrefix, propertyName, field);
            return ErrorCatalog.GeneralError.Enum.InEnum(fieldDisplay, enumName, value, code, isInvariant, message);
        }

        #endregion
        
        #region Compare

        public static Error Equal<TProperty>(
            TProperty property,
            TProperty value,
            bool isInvariant = false,
            string? codePrefix = null,
            string propertyName = "",
            string? field = null, 
            string? message = null,
            [CallerArgumentExpression("property")] string expr = "") 
        {
            var (fieldDisplay, code) = ResolveFieldAndCode(codePrefix, propertyName, field, expr);
            return ErrorCatalog.GeneralError.Compare.Equal(fieldDisplay, value, code, isInvariant, message);
        }
        
        public static Error Equal<TOwner, TProperty>(
            Expression<Func<TOwner, TProperty>> exprPropertyName,
            TProperty value,
            bool isInvariant = false,
            string? codePrefix = null,
            string? propertyName = null,
            string? field = null, 
            string? message = null)
        {
            var (fieldDisplay, code) = ResolveFieldAndCode(exprPropertyName, codePrefix, propertyName, field);
            return ErrorCatalog.GeneralError.Compare.Equal(fieldDisplay, value, code, isInvariant, message);
        }

        public static Error NotEqual<TProperty>(
            TProperty property,
            TProperty value,
            bool isInvariant = false,
            string? codePrefix = null,
            string propertyName = "",
            string? field = null, 
            string? message = null,
            [CallerArgumentExpression("property")] string expr = "")
        {
            var (fieldDisplay, code) = ResolveFieldAndCode(codePrefix, propertyName, field, expr);
            return ErrorCatalog.GeneralError.Compare.NotEqual(fieldDisplay, value, code, isInvariant, message);
        }
        
        public static Error NotEqual<TOwner, TProperty>(
            Expression<Func<TOwner, TProperty>> exprPropertyName,
            TProperty value,
            bool isInvariant = false,
            string? codePrefix = null,
            string? propertyName = null,
            string? field = null, 
            string? message = null)
        {
            var (fieldDisplay, code) = ResolveFieldAndCode(exprPropertyName, codePrefix, propertyName, field);
            return ErrorCatalog.GeneralError.Compare.NotEqual(fieldDisplay, value, code, isInvariant, message);
        }

        public static Error BetweenInclusive<TProperty>(
            TProperty property,
            TProperty min,
            TProperty max,
            bool isInvariant = false,
            string? codePrefix = null,
            string propertyName = "",
            string? field = null, 
            string? message = null,
            [CallerArgumentExpression("property")] string expr = "")
        {
            var (fieldDisplay, code) = ResolveFieldAndCode(codePrefix, propertyName, field, expr);
            return ErrorCatalog.GeneralError.Compare.BetweenInclusive(fieldDisplay, min, max, code, isInvariant, message);
        }
        
        public static Error BetweenInclusive<TOwner, TProperty>(
            Expression<Func<TOwner, TProperty>> exprPropertyName,
            TProperty min,
            TProperty max,
            bool isInvariant = false,
            string? codePrefix = null,
            string? propertyName = null,
            string? field = null, 
            string? message = null)
        {
            var (fieldDisplay, code) = ResolveFieldAndCode(exprPropertyName, codePrefix, propertyName, field);
            return ErrorCatalog.GeneralError.Compare.BetweenInclusive(fieldDisplay, min, max, code, isInvariant, message);
        }

        public static Error BetweenExclusive<TProperty>(
            TProperty property,
            TProperty min,
            TProperty max,
            bool isInvariant = false,
            string? codePrefix = null,
            string propertyName = "",
            string? field = null, 
            string? message = null,
            [CallerArgumentExpression("property")] string expr = "")
        {
            var (fieldDisplay, code) = ResolveFieldAndCode(codePrefix, propertyName, field, expr);
            return ErrorCatalog.GeneralError.Compare.BetweenExclusive(fieldDisplay, min, max, code, isInvariant, message);
        }
        
        public static Error BetweenExclusive<TOwner, TProperty>(
            Expression<Func<TOwner, TProperty>> exprPropertyName,
            TProperty min,
            TProperty max,
            bool isInvariant = false,
            string? codePrefix = null,
            string? propertyName = null,
            string? field = null, 
            string? message = null)
        {
            var (fieldDisplay, code) = ResolveFieldAndCode(exprPropertyName, codePrefix, propertyName, field);
            return ErrorCatalog.GeneralError.Compare.BetweenExclusive(fieldDisplay, min, max, code, isInvariant, message);
        }
        
        public static Error GreaterThan<TProperty>(
            TProperty property,
            TProperty value,
            bool isInvariant = false,
            string? codePrefix = null,
            string propertyName = "",
            string? field = null, 
            string? message = null,
            [CallerArgumentExpression("property")] string expr = "")
        {
            var (fieldDisplay, code) = ResolveFieldAndCode(codePrefix, propertyName, field, expr);
            return ErrorCatalog.GeneralError.Compare.GreaterThan(fieldDisplay, value, code, isInvariant, message);
        }
        
        public static Error GreaterThan<TOwner, TProperty>(
            Expression<Func<TOwner, TProperty>> exprPropertyName,
            TProperty value,
            bool isInvariant = false,
            string? codePrefix = null,
            string? propertyName = null,
            string? field = null, 
            string? message = null)
        {
            var (fieldDisplay, code) = ResolveFieldAndCode(exprPropertyName, codePrefix, propertyName, field);
            return ErrorCatalog.GeneralError.Compare.GreaterThan(fieldDisplay, value, code, isInvariant, message);
        }
        
        public static Error GreaterThanOrEqual<TProperty>(
            TProperty property,
            TProperty value,
            bool isInvariant = false,
            string? codePrefix = null,
            string propertyName = "",
            string? field = null, 
            string? message = null,
            [CallerArgumentExpression("property")] string expr = "")
        {
            var (fieldDisplay, code) = ResolveFieldAndCode(codePrefix, propertyName, field, expr);
            return ErrorCatalog.GeneralError.Compare.GreaterThanOrEqual(fieldDisplay, value, code, isInvariant, message);
        }
        
        public static Error GreaterThanOrEqual<TOwner, TProperty>(
            Expression<Func<TOwner, TProperty>> exprPropertyName,
            TProperty value,
            bool isInvariant = false,
            string? codePrefix = null,
            string? propertyName = null,
            string? field = null, 
            string? message = null)
        {
            var (fieldDisplay, code) = ResolveFieldAndCode(exprPropertyName, codePrefix, propertyName, field);
            return ErrorCatalog.GeneralError.Compare.GreaterThanOrEqual(fieldDisplay, value, code, isInvariant, message);
        }
        
        public static Error LessThan<TProperty>(
            TProperty property,
            TProperty value,
            bool isInvariant = false,
            string? codePrefix = null,
            string propertyName = "",
            string? field = null, 
            string? message = null,
            [CallerArgumentExpression("property")] string expr = "")
        {
            var (fieldDisplay, code) = ResolveFieldAndCode(codePrefix, propertyName, field, expr);
            return ErrorCatalog.GeneralError.Compare.LessThan(fieldDisplay, value, code, isInvariant, message);
        }
        
        public static Error LessThan<TOwner, TProperty>(
            Expression<Func<TOwner, TProperty>> exprPropertyName,
            TProperty value,
            bool isInvariant = false,
            string? codePrefix = null,
            string? propertyName = null,
            string? field = null, 
            string? message = null)
        {
            var (fieldDisplay, code) = ResolveFieldAndCode(exprPropertyName, codePrefix, propertyName, field);
            return ErrorCatalog.GeneralError.Compare.LessThan(fieldDisplay, value, code, isInvariant, message);
        }
        
        public static Error LessThanOrEqual<TProperty>(
            TProperty property,
            TProperty value,
            bool isInvariant = false,
            string? codePrefix = null,
            string propertyName = "",
            string? field = null, 
            string? message = null,
            [CallerArgumentExpression("property")] string expr = "")
        {
            var (fieldDisplay, code) = ResolveFieldAndCode(codePrefix, propertyName, field, expr);
            return ErrorCatalog.GeneralError.Compare.LessThanOrEqual(fieldDisplay, value, code, isInvariant, message);
        }
        
        public static Error LessThanOrEqual<TOwner, TProperty>(
            Expression<Func<TOwner, TProperty>> exprPropertyName,
            TProperty value,
            bool isInvariant = false,
            string? codePrefix = null,
            string? propertyName = null,
            string? field = null, 
            string? message = null)
        {
            var (fieldDisplay, code) = ResolveFieldAndCode(exprPropertyName, codePrefix, propertyName, field);
            return ErrorCatalog.GeneralError.Compare.LessThanOrEqual(fieldDisplay, value, code, isInvariant, message);
        }

        #endregion
        
        #region Time

        public static Error After<TProperty>(
            TProperty property,
            string time,
            bool isInvariant = false,
            string? codePrefix = null,
            string propertyName = "",
            string? field = null, 
            string? message = null,
            [CallerArgumentExpression("property")] string expr = "")
        {
            var (fieldDisplay, code) = ResolveFieldAndCode(codePrefix, propertyName, field, expr);
            return ErrorCatalog.GeneralError.Time.After(fieldDisplay, time, code, isInvariant, message);
        }
        
        public static Error After<TOwner, TProperty>(
            Expression<Func<TOwner, TProperty>> exprPropertyName,
            string time,
            bool isInvariant = false,
            string? codePrefix = null,
            string? propertyName = null,
            string? field = null, 
            string? message = null)
        {
            var (fieldDisplay, code) = ResolveFieldAndCode(exprPropertyName, codePrefix, propertyName, field);
            return ErrorCatalog.GeneralError.Time.After(fieldDisplay, time, code, isInvariant, message);
        }

        public static Error Before<TProperty>(
            TProperty property,
            string time,
            bool isInvariant = false,
            string? codePrefix = null,
            string propertyName = "",
            string? field = null, 
            string? message = null,
            [CallerArgumentExpression("property")] string expr = "")
        {
            var (fieldDisplay, code) = ResolveFieldAndCode(codePrefix, propertyName, field, expr);
            return ErrorCatalog.GeneralError.Time.Before(fieldDisplay, time, code, isInvariant, message);
        }
        
        public static Error Before<TOwner, TProperty>(
            Expression<Func<TOwner, TProperty>> exprPropertyName,
            string time,
            bool isInvariant = false,
            string? codePrefix = null,
            string? propertyName = null,
            string? field = null, 
            string? message = null)
        {
            var (fieldDisplay, code) = ResolveFieldAndCode(exprPropertyName, codePrefix, propertyName, field);
            return ErrorCatalog.GeneralError.Time.Before(fieldDisplay, time, code, isInvariant, message);
        }

        public static Error TimeRange<TProperty>(
            TProperty property,
            string timeStart,
            string timeEnd,
            bool isInvariant = false,
            string? codePrefix = null,
            string propertyName = "",
            string? field = null, 
            string? message = null,
            [CallerArgumentExpression("property")] string expr = "")
        {
            var (fieldDisplay, code) = ResolveFieldAndCode(codePrefix, propertyName, field, expr);
            return ErrorCatalog.GeneralError.Time.TimeRange(fieldDisplay, timeStart, timeEnd, code, isInvariant, message);
        }
        
        public static Error TimeRange<TOwner, TProperty>(
            Expression<Func<TOwner, TProperty>> exprPropertyName,
            string timeStart,
            string timeEnd,
            bool isInvariant = false,
            string? codePrefix = null,
            string? propertyName = null,
            string? field = null, 
            string? message = null)
        {
            var (fieldDisplay, code) = ResolveFieldAndCode(exprPropertyName, codePrefix, propertyName, field);
            return ErrorCatalog.GeneralError.Time.TimeRange(fieldDisplay, timeStart, timeEnd, code, isInvariant, message);
        }

        public static Error NotInPast<TProperty>(
            TProperty property,
            bool isInvariant = false,
            string? codePrefix = null,
            string propertyName = "",
            string? field = null, 
            string? message = null,
            [CallerArgumentExpression("property")] string expr = "")
        {
            var (fieldDisplay, code) = ResolveFieldAndCode(codePrefix, propertyName, field, expr);
            return ErrorCatalog.GeneralError.Time.NotInPast(fieldDisplay, code, isInvariant, message);
        }
        
        public static Error NotInPast<TOwner, TProperty>(
            Expression<Func<TOwner, TProperty>> exprPropertyName,
            bool isInvariant = false,
            string? codePrefix = null,
            string? propertyName = null,
            string? field = null, 
            string? message = null)
        {
            var (fieldDisplay, code) = ResolveFieldAndCode(exprPropertyName, codePrefix, propertyName, field);
            return ErrorCatalog.GeneralError.Time.NotInPast(fieldDisplay, code, isInvariant, message);
        }
        
        public static Error NotInFuture<TProperty>(
            TProperty property,
            bool isInvariant = false,
            string? codePrefix = null,
            string propertyName = "",
            string? field = null, 
            string? message = null,
            [CallerArgumentExpression("property")] string expr = "")
        {
            var (fieldDisplay, code) = ResolveFieldAndCode(codePrefix, propertyName, field, expr);
            return ErrorCatalog.GeneralError.Time.NotInFuture(fieldDisplay, code, isInvariant, message);
        }
        
        public static Error NotInFuture<TOwner, TProperty>(
            Expression<Func<TOwner, TProperty>> exprPropertyName,
            bool isInvariant = false,
            string? codePrefix = null,
            string? propertyName = null,
            string? field = null, 
            string? message = null)
        {
            var (fieldDisplay, code) = ResolveFieldAndCode(exprPropertyName, codePrefix, propertyName, field);
            return ErrorCatalog.GeneralError.Time.NotInFuture(fieldDisplay, code, isInvariant, message);
        }
        
        public static Error Utc<TProperty>(
            TProperty property,
            bool isInvariant = false,
            string? codePrefix = null,
            string propertyName = "",
            string? field = null, 
            string? message = null,
            [CallerArgumentExpression("property")] string expr = "")
        {
            var (fieldDisplay, code) = ResolveFieldAndCode(codePrefix, propertyName, field, expr);
            return ErrorCatalog.GeneralError.Time.Utc(fieldDisplay, code, isInvariant, message);
        }
        
        public static Error Utc<TOwner, TProperty>(
            Expression<Func<TOwner, TProperty>> exprPropertyName,
            bool isInvariant = false,
            string? codePrefix = null,
            string? propertyName = null,
            string? field = null, 
            string? message = null)
        {
            var (fieldDisplay, code) = ResolveFieldAndCode(exprPropertyName, codePrefix, propertyName, field);
            return ErrorCatalog.GeneralError.Time.Utc(fieldDisplay, code, isInvariant, message);
        }
        
        public static Error NotOverlapping<TProperty>(
            TProperty property,
            bool isInvariant = false,
            string? codePrefix = null,
            string propertyName = "",
            string? field = null, 
            string? message = null,
            [CallerArgumentExpression("property")] string expr = "")
        {
            var (fieldDisplay, code) = ResolveFieldAndCode(codePrefix, propertyName, field, expr);
            return ErrorCatalog.GeneralError.Time.NotOverlapping(fieldDisplay, code, isInvariant, message);
        }
        
        public static Error NotOverlapping<TOwner, TProperty>(
            Expression<Func<TOwner, TProperty>> exprPropertyName,
            bool isInvariant = false,
            string? codePrefix = null,
            string? propertyName = null,
            string? field = null, 
            string? message = null)
        {
            var (fieldDisplay, code) = ResolveFieldAndCode(exprPropertyName, codePrefix, propertyName, field);
            return ErrorCatalog.GeneralError.Time.NotOverlapping(fieldDisplay, code, isInvariant, message);
        }
        
        #endregion
    }
    
    public static int GetStatus(this Error error)
    {
        return error.Kind switch
        {
            ErrorCatalog.Kind.Conflict => StatusCodes.Status409Conflict,
            ErrorCatalog.Kind.Validation => StatusCodes.Status400BadRequest,
            ErrorCatalog.Kind.Unauthorized => StatusCodes.Status401Unauthorized,
            ErrorCatalog.Kind.Forbidden => StatusCodes.Status403Forbidden,
            ErrorCatalog.Kind.NotFound => StatusCodes.Status404NotFound,
            ErrorCatalog.Kind.Invariant => StatusCodes.Status422UnprocessableEntity,
            ErrorCatalog.Kind.Violations => StatusCodes.Status422UnprocessableEntity,
            ErrorCatalog.Kind.Unexpected => StatusCodes.Status500InternalServerError,
            ErrorCatalog.Kind.Unavailable => StatusCodes.Status503ServiceUnavailable,
            ErrorCatalog.Kind.Timeout => StatusCodes.Status504GatewayTimeout,
            _ => StatusCodes.Status500InternalServerError
        };
    }
    
    private static (string FieldDisplay, string Code) ResolveFieldAndCode<T, TProperty>(
        Expression<Func<T, TProperty>> exprPropertyName,
        string? codePrefix = null,
        string? propertyName = null,
        string? field = null)
    {
        const string separator = Constant.DefaultErrorCodeSeparator;
            
        var propertyPath = exprPropertyName.GetOrAddName();
            
        field ??= propertyPath.ExtractLastMember();
            
        var prefix = codePrefix ?? typeof(T).Name;
            
        var nameForCode = propertyName ?? propertyPath;
            
        var code = BuildCode(prefix, nameForCode, separator);
        
        return (field, code);
    }

    private static (string FieldDisplay, string Code) ResolveFieldAndCode(
        string? codePrefix = null,
        string propertyName = "",
        string? field = null,
        string expr = ""
    )
    {
        const string separator = Constant.DefaultErrorCodeSeparator;
        
        field ??= expr.ExtractLastMember();
        
        codePrefix ??= expr;
        
        var code = BuildCode(codePrefix, propertyName,separator);
        
        return (field, code);
    }
    
    private static string BuildCode(string prefix, string name, string sep)
    {
        return string.Join(sep, Tokens(prefix).Concat(Tokens(name)));

        static IEnumerable<string> Tokens(string s) =>
            s.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }
}