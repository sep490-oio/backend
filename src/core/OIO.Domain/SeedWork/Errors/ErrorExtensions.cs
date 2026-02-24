using System.Collections;
using System.Linq.Expressions;
using System.Numerics;
using System.Runtime.CompilerServices;
using OIO.Domain.SeedWork.Checks;
using OIO.Domain.SeedWork.Errors.ErrorCatalogs;
using OIO.Domain.SeedWork.Utils;

namespace OIO.Domain.SeedWork.Errors;

public static class ErrorExtensions
{
    #region Required

        public static Error NotNullError<TProperty>(
            this TProperty _,
            bool isInvariant = false,
            string? codePrefix = null,
            string propertyName = "",
            string? field = null, 
            string? message = null,
            [CallerArgumentExpression("_")] string expr = "")
        {
            var (fieldDisplay, code) = ResolveFieldAndCode(codePrefix, propertyName, field, expr);
            return ErrorCatalog.GeneralError.Required.NotNull(fieldDisplay, code, isInvariant, message);
        }
        
        public static Error NotEmptyError<TProperty>(
            this TProperty _,
            bool isInvariant = false,
            string? codePrefix = null,
            string propertyName = "",
            string? field = null, 
            string? message = null,
            [CallerArgumentExpression("_")] string expr = "")
        {
            var (fieldDisplay, code) = ResolveFieldAndCode(codePrefix, propertyName, field, expr);
            return ErrorCatalog.GeneralError.Required.NotEmpty(fieldDisplay, code, isInvariant, message);
        }
        
        public static Error NotEmptyGuidError<TProperty>(
            this TProperty _,
            bool isInvariant = false,
            string? codePrefix = null,
            string propertyName = "",
            string? field = null, 
            string? message = null,
            [CallerArgumentExpression("_")] string expr = "")
        {
            var (fieldDisplay, code) = ResolveFieldAndCode(codePrefix, propertyName, field, expr);
            return ErrorCatalog.GeneralError.Required.NotEmptyGuid(fieldDisplay, code, isInvariant, message);
        }
        
        public static Error NotDefaultError<TProperty>(
            this TProperty? _,
            TProperty? @default,
            bool isInvariant = false,
            string? codePrefix = null,
            string propertyName = "",
            string? field = null, 
            string? message = null,
            [CallerArgumentExpression("_")] string expr = "") where TProperty  : struct
        {
            var (fieldDisplay, code) = ResolveFieldAndCode(codePrefix, propertyName, field, expr);
            return ErrorCatalog.GeneralError.Required.NotDefault(fieldDisplay, @default, code, isInvariant, message);
        }
        
        public static Error NotDefaultError<TProperty>(
            this TProperty _,
            TProperty? @default,
            bool isInvariant = false,
            string? codePrefix = null,
            string propertyName = "",
            string? field = null, 
            string? message = null,
            [CallerArgumentExpression("_")] string expr = "") where TProperty : struct
        {
            var (fieldDisplay, code) = ResolveFieldAndCode(codePrefix, propertyName, field, expr);
            return ErrorCatalog.GeneralError.Required.NotDefault(fieldDisplay, @default, code, isInvariant, message);
        }
        
        public static Error NotDefaultError<TProperty>(
            this TProperty? _,
            TProperty? @default,
            bool isInvariant = false,
            string? codePrefix = null,
            string propertyName = "",
            string? field = null, 
            string? message = null,
            [CallerArgumentExpression("_")] string expr = "")
        {
            var (fieldDisplay, code) = ResolveFieldAndCode(codePrefix, propertyName, field, expr);
            return ErrorCatalog.GeneralError.Required.NotDefault(fieldDisplay, @default, code, isInvariant, message);
        }
        
        public static Error NotBlankError<TProperty>(
            this TProperty _,
            bool isInvariant = false,
            string? codePrefix = null,
            string propertyName = "",
            string? field = null, 
            string? message = null,
            [CallerArgumentExpression("_")] string expr = "")
        {
            var (fieldDisplay, code) = ResolveFieldAndCode(codePrefix, propertyName, field, expr);
            return ErrorCatalog.GeneralError.Required.NotBlank(fieldDisplay, code, isInvariant, message);
        }

        #endregion

        #region Text

        public static Error MatchesError(
            this string _,
            string pattern,
            bool isInvariant = false,
            string? codePrefix = null,
            string propertyName = "",
            string? field = null, 
            string? message = null,
            [CallerArgumentExpression("_")] string expr = "")
        {
            var (fieldDisplay, code) = ResolveFieldAndCode(codePrefix, propertyName, field, expr);
            return ErrorCatalog.GeneralError.Text.Matches(fieldDisplay, pattern, code, isInvariant, message);
        }
        
        public static Error StartsWithError(
            this string _,
            string prefix,
            bool isInvariant = false,
            string? codePrefix = null,
            string propertyName = "",
            string? field = null, 
            string? message = null,
            [CallerArgumentExpression("_")] string expr = "")
        {
            var (fieldDisplay, code) = ResolveFieldAndCode(codePrefix, propertyName, field, expr);
            return ErrorCatalog.GeneralError.Text.StartsWith(fieldDisplay, prefix, code, isInvariant, message);
        }
        
        public static Error EndsWithError(
            this string _,
            string suffix,
            bool isInvariant = false,
            string? codePrefix = null,
            string propertyName = "",
            string? field = null, 
            string? message = null,
            [CallerArgumentExpression("_")] string expr = "")
        {
            var (fieldDisplay, code) = ResolveFieldAndCode(codePrefix, propertyName, field, expr);
            return ErrorCatalog.GeneralError.Text.EndsWith(fieldDisplay, suffix, code, isInvariant, message);
        }
        
        public static Error ContainsError(
            this string _,
            string substring,
            bool isInvariant = false,
            string? codePrefix = null,
            string propertyName = "",
            string? field = null, 
            string? message = null,
            [CallerArgumentExpression("_")] string expr = "")
        {
            var (fieldDisplay, code) = ResolveFieldAndCode(codePrefix, propertyName, field, expr);
            return ErrorCatalog.GeneralError.Text.Contains(fieldDisplay, substring, code, isInvariant, message);
        }
        
        public static Error MaxLengthError(
            this string _,
            int max,
            bool isInvariant = false,
            string? codePrefix = null,
            string propertyName = "",
            string? field = null, 
            string? message = null,
            [CallerArgumentExpression("_")] string expr = "")
        {
            var (fieldDisplay, code) = ResolveFieldAndCode(codePrefix, propertyName, field, expr);
            return ErrorCatalog.GeneralError.Text.MaxLength(fieldDisplay, max, code, isInvariant, message);
        }
        
        public static Error MinLengthError(
            this string _,
            int min,
            bool isInvariant = false,
            string? codePrefix = null,
            string propertyName = "",
            string? field = null, 
            string? message = null,
            [CallerArgumentExpression("_")] string expr = "")
        {
            var (fieldDisplay, code) = ResolveFieldAndCode(codePrefix, propertyName, field, expr);
            return ErrorCatalog.GeneralError.Text.MinLength(fieldDisplay, min, code, isInvariant, message);
        }
        
        public static Error ExactLengthError(
            this string _,
            int length,
            bool isInvariant = false,
            string? codePrefix = null,
            string propertyName = "",
            string? field = null, 
            string? message = null,
            [CallerArgumentExpression("_")] string expr = "")
        {
            var (fieldDisplay, code) = ResolveFieldAndCode(codePrefix, propertyName, field, expr);
            return ErrorCatalog.GeneralError.Text.ExactLength(fieldDisplay, length, code, isInvariant, message);
        }
        
        public static Error LengthBetweenError(
            this string _,
            int min,
            int max,
            bool isInvariant = false,
            string? codePrefix = null,
            string propertyName = "",
            string? field = null, 
            string? message = null,
            [CallerArgumentExpression("_")] string expr = "")
        {
            var (fieldDisplay, code) = ResolveFieldAndCode(codePrefix, propertyName, field, expr);
            return ErrorCatalog.GeneralError.Text.LengthBetween(fieldDisplay, min, max, code, isInvariant, message);
        }
        
        public static Error FormatError(
            this string _,
            bool isInvariant = false,
            string? codePrefix = null,
            string propertyName = "",
            string? field = null, 
            string? message = null,
            [CallerArgumentExpression("_")] string expr = "")
        {
            var (fieldDisplay, code) = ResolveFieldAndCode(codePrefix, propertyName, field, expr);
            return ErrorCatalog.GeneralError.Text.Format(fieldDisplay, code, isInvariant, message);
        }
        
        #endregion

        #region Numberic

        public static Error PositiveError<TProperty>(
            this TProperty _,
            bool isInvariant = false,
            string? codePrefix = null,
            string propertyName = "",
            string? field = null, 
            string? message = null,
            [CallerArgumentExpression("_")] string expr = "") where  TProperty : INumber<TProperty>
        {
            var (fieldDisplay, code) = ResolveFieldAndCode(codePrefix, propertyName, field, expr);
            return ErrorCatalog.GeneralError.Numeric.Positive(fieldDisplay, code, isInvariant, message);
        }

        public static Error NegativeError<TProperty>(
            this TProperty _,
            bool isInvariant = false,
            string? codePrefix = null,
            string propertyName = "",
            string? field = null, 
            string? message = null,
            [CallerArgumentExpression("_")] string expr = "") where  TProperty : INumber<TProperty>
        {
            var (fieldDisplay, code) = ResolveFieldAndCode(codePrefix, propertyName, field, expr);
            return ErrorCatalog.GeneralError.Numeric.Negative(fieldDisplay, code, isInvariant, message);
        }

        public static Error NonPositiveError<TProperty>(
            this TProperty _,
            bool isInvariant = false,
            string? codePrefix = null,
            string propertyName = "",
            string? field = null, 
            string? message = null,
            [CallerArgumentExpression("_")] string expr = "") where  TProperty : INumber<TProperty>
        {
            var (fieldDisplay, code) = ResolveFieldAndCode(codePrefix, propertyName, field, expr);
            return ErrorCatalog.GeneralError.Numeric.NonPositive(fieldDisplay, code, isInvariant, message);
        }

        public static Error NonNegativeError<TProperty>(
            this TProperty _,
            bool isInvariant = false,
            string? codePrefix = null,
            string propertyName = "",
            string? field = null, 
            string? message = null,
            [CallerArgumentExpression("_")] string expr = "") where  TProperty : INumber<TProperty>
        {
            var (fieldDisplay, code) = ResolveFieldAndCode(codePrefix, propertyName, field, expr);
            return ErrorCatalog.GeneralError.Numeric.NonNegative(fieldDisplay, code, isInvariant, message);
        }

        public static Error ZeroError<TProperty>(
            this TProperty _,
            bool isInvariant = false,
            string? codePrefix = null,
            string propertyName = "",
            string? field = null, 
            string? message = null,
            [CallerArgumentExpression("_")] string expr = "") where  TProperty : INumber<TProperty>
        {
            var (fieldDisplay, code) = ResolveFieldAndCode(codePrefix, propertyName, field, expr);
            return ErrorCatalog.GeneralError.Numeric.Zero(fieldDisplay, code, isInvariant, message);
        }

        public static Error NonZeroError<TProperty>(
            this TProperty _,
            bool isInvariant = false,
            string? codePrefix = null,
            string propertyName = "",
            string? field = null, 
            string? message = null,
            [CallerArgumentExpression("_")] string expr = "") where  TProperty : INumber<TProperty>
        {
            var (fieldDisplay, code) = ResolveFieldAndCode(codePrefix, propertyName, field, expr);
            return ErrorCatalog.GeneralError.Numeric.NonZero(fieldDisplay, code, isInvariant, message);
        }

        public static Error MultipleOfError<TProperty>(
            this TProperty _,
            TProperty step,
            bool isInvariant = false,
            string? codePrefix = null,
            string propertyName = "",
            string? field = null, 
            string? message = null,
            [CallerArgumentExpression("_")] string expr = "") where TProperty : INumber<TProperty>
        {
            var (fieldDisplay, code) = ResolveFieldAndCode(codePrefix, propertyName, field, expr);
            return ErrorCatalog.GeneralError.Numeric.MultipleOf(fieldDisplay, step, code, isInvariant, message);
        }
        

        public static Error PrecisionScaleError<TProperty>(
            this TProperty _,
            int precision,
            int scale,
            bool isInvariant = false,
            string? codePrefix = null,
            string propertyName = "",
            string? field = null, 
            string? message = null,
            [CallerArgumentExpression("_")] string expr = "") where TProperty : INumber<TProperty>
        {
            var (fieldDisplay, code) = ResolveFieldAndCode(codePrefix, propertyName, field, expr);
            return ErrorCatalog.GeneralError.Numeric.PrecisionScale(fieldDisplay, precision, scale, code, isInvariant, message);
        }

        #endregion

        #region Collection

        public static Error NotContainsError<TProperty>(
            this TProperty _,
            string item,
            bool isInvariant = false,
            string? codePrefix = null,
            string propertyName = "",
            string? field = null, 
            string? message = null,
            [CallerArgumentExpression("_")] string expr = "") where TProperty : IEnumerable?
        {
            var (fieldDisplay, code) = ResolveFieldAndCode(codePrefix, propertyName, field, expr);
            return ErrorCatalog.GeneralError.Collection.NotContains(fieldDisplay, item, code, isInvariant, message);
        }

        public static Error NotInSetError<TProperty>(
            this TProperty _,
            string setString,
            bool isInvariant = false,
            string? codePrefix = null,
            string propertyName = "",
            string? field = null, 
            string? message = null,
            [CallerArgumentExpression("_")] string expr = "") where TProperty : IEnumerable?
        {
            var (fieldDisplay, code) = ResolveFieldAndCode(codePrefix, propertyName, field, expr);
            return ErrorCatalog.GeneralError.Collection.NotInSet(fieldDisplay, setString, code, isInvariant, message);
        }

        public static Error InSetError<TProperty>(
            this TProperty _,
            string setString,
            bool isInvariant = false,
            string? codePrefix = null,
            string propertyName = "",
            string? field = null, 
            string? message = null,
            [CallerArgumentExpression("_")] string expr = "") where TProperty : IEnumerable?
        {
            var (fieldDisplay, code) = ResolveFieldAndCode(codePrefix, propertyName, field, expr);
            return ErrorCatalog.GeneralError.Collection.InSet(fieldDisplay, setString, code, isInvariant, message);
        }

        public static Error NoDuplicatesError<TProperty>(
            this TProperty _,
            string duplicate,
            bool isInvariant = false,
            string? codePrefix = null,
            string propertyName = "",
            string? field = null, 
            string? message = null,
            [CallerArgumentExpression("_")] string expr = "") where TProperty : IEnumerable?
        {
            var (fieldDisplay, code) = ResolveFieldAndCode(codePrefix, propertyName, field, expr);
            return ErrorCatalog.GeneralError.Collection.NoDuplicates(fieldDisplay, duplicate, code, isInvariant, message);
        }

        public static Error CountMinError<TProperty>(
            this TProperty _,
            int min,
            bool isInvariant = false,
            string? codePrefix = null,
            string propertyName = "",
            string? field = null, 
            string? message = null,
            [CallerArgumentExpression("_")] string expr = "") where TProperty : IEnumerable
        {
            var (fieldDisplay, code) = ResolveFieldAndCode(codePrefix, propertyName, field, expr);
            return ErrorCatalog.GeneralError.Collection.CountMin(fieldDisplay, min, code, isInvariant, message);
        }

        public static Error CountMaxError<TProperty>(
            this TProperty _,
            int max,
            bool isInvariant = false,
            string? codePrefix = null,
            string propertyName = "",
            string? field = null, 
            string? message = null,
            [CallerArgumentExpression("_")] string expr = "") where TProperty : IEnumerable
        {
            var (fieldDisplay, code) = ResolveFieldAndCode(codePrefix, propertyName, field, expr);
            return ErrorCatalog.GeneralError.Collection.CountMax(fieldDisplay, max, code, isInvariant, message);
        }

        public static Error CountBetweenError<TProperty>(
            this TProperty _,
            int min,
            int max,
            bool isInvariant = false,
            string? codePrefix = null,
            string propertyName = "",
            string? field = null, 
            string? message = null,
            [CallerArgumentExpression("_")] string expr = "") where TProperty : IEnumerable
        {
            var (fieldDisplay, code) = ResolveFieldAndCode(codePrefix, propertyName, field, expr);
            return ErrorCatalog.GeneralError.Collection.CountBetween(fieldDisplay, min, max, code, isInvariant, message);
        }

        #endregion

        #region Scalar

        public static Error ScalarNotInSetError<TProperty>(
            this TProperty _,
            string setString,
            bool isInvariant = false,
            string? codePrefix = null,
            string propertyName = "",
            string? field = null, 
            string? message = null,
            [CallerArgumentExpression("_")] string expr = "")
        {
            var (fieldDisplay, code) = ResolveFieldAndCode(codePrefix, propertyName, field, expr);
            return ErrorCatalog.GeneralError.Scalar.NotInSet(fieldDisplay, setString, code, isInvariant, message);
        }
    
        public static Error ScalarInSetError<TProperty>(
            this TProperty _,
            string setString,
            bool isInvariant = false,
            string? codePrefix = null,
            string propertyName = "",
            string? field = null, 
            string? message = null,
            [CallerArgumentExpression("_")] string expr = "")
        {
            var (fieldDisplay, code) = ResolveFieldAndCode(codePrefix, propertyName, field, expr);
            return ErrorCatalog.GeneralError.Scalar.InSet(fieldDisplay, setString, code, isInvariant, message);
        }

        #endregion

        #region Enum

        public static Error InEnumError<TProperty>(
            this TProperty _,
            string enumName,
            string value,
            bool isInvariant = false,
            string? codePrefix = null,
            string propertyName = "",
            string? field = null, 
            string? message = null,
            [CallerArgumentExpression("_")] string expr = "")
        {
            var (fieldDisplay, code) = ResolveFieldAndCode(codePrefix, propertyName, field, expr);
            return ErrorCatalog.GeneralError.Enum.InEnum(fieldDisplay, enumName, value, code, isInvariant, message);
        }

        #endregion

        
        #region Compare

        public static Error EqualError<TProperty>(
            this TProperty _,
            TProperty value,
            bool isInvariant = false,
            string? codePrefix = null,
            string propertyName = "",
            string? field = null, 
            string? message = null,
            [CallerArgumentExpression("_")] string expr = "") 
        {
            var (fieldDisplay, code) = ResolveFieldAndCode(codePrefix, propertyName, field, expr);
            return ErrorCatalog.GeneralError.Compare.Equal(fieldDisplay, value, code, isInvariant, message);
        }

        public static Error NotEqualError<TProperty>(
            this TProperty _,
            TProperty value,
            bool isInvariant = false,
            string? codePrefix = null,
            string propertyName = "",
            string? field = null, 
            string? message = null,
            [CallerArgumentExpression("_")] string expr = "")
        {
            var (fieldDisplay, code) = ResolveFieldAndCode(codePrefix, propertyName, field, expr);
            return ErrorCatalog.GeneralError.Compare.NotEqual(fieldDisplay, value, code, isInvariant, message);
        }

        public static Error BetweenInclusiveError<TProperty>(
            this TProperty _,
            TProperty min,
            TProperty max,
            bool isInvariant = false,
            string? codePrefix = null,
            string propertyName = "",
            string? field = null, 
            string? message = null,
            [CallerArgumentExpression("_")] string expr = "")
        {
            var (fieldDisplay, code) = ResolveFieldAndCode(codePrefix, propertyName, field, expr);
            return ErrorCatalog.GeneralError.Compare.BetweenInclusive(fieldDisplay, min, max, code, isInvariant, message);
        }

        public static Error BetweenExclusiveError<TProperty>(
            this TProperty _,
            TProperty min,
            TProperty max,
            bool isInvariant = false,
            string? codePrefix = null,
            string propertyName = "",
            string? field = null, 
            string? message = null,
            [CallerArgumentExpression("_")] string expr = "")
        {
            var (fieldDisplay, code) = ResolveFieldAndCode(codePrefix, propertyName, field, expr);
            return ErrorCatalog.GeneralError.Compare.BetweenExclusive(fieldDisplay, min, max, code, isInvariant, message);
        }
        
        public static Error GreaterThanError<TProperty>(
            this TProperty _,
            TProperty value,
            bool isInvariant = false,
            string? codePrefix = null,
            string propertyName = "",
            string? field = null, 
            string? message = null,
            [CallerArgumentExpression("_")] string expr = "")
        {
            var (fieldDisplay, code) = ResolveFieldAndCode(codePrefix, propertyName, field, expr);
            return ErrorCatalog.GeneralError.Compare.GreaterThan(fieldDisplay, value, code, isInvariant, message);
        }
        
        public static Error GreaterThanOrEqualError<TProperty>(
            this TProperty _,
            TProperty value,
            bool isInvariant = false,
            string? codePrefix = null,
            string propertyName = "",
            string? field = null, 
            string? message = null,
            [CallerArgumentExpression("_")] string expr = "")
        {
            var (fieldDisplay, code) = ResolveFieldAndCode(codePrefix, propertyName, field, expr);
            return ErrorCatalog.GeneralError.Compare.GreaterThanOrEqual(fieldDisplay, value, code, isInvariant, message);
        }
        
        public static Error LessThanError<TProperty>(
            this TProperty _,
            TProperty value,
            bool isInvariant = false,
            string? codePrefix = null,
            string propertyName = "",
            string? field = null, 
            string? message = null,
            [CallerArgumentExpression("_")] string expr = "")
        {
            var (fieldDisplay, code) = ResolveFieldAndCode(codePrefix, propertyName, field, expr);
            return ErrorCatalog.GeneralError.Compare.LessThan(fieldDisplay, value, code, isInvariant, message);
        }
        
        public static Error LessThanOrEqualError<TProperty>(
            this TProperty _,
            TProperty value,
            bool isInvariant = false,
            string? codePrefix = null,
            string propertyName = "",
            string? field = null, 
            string? message = null,
            [CallerArgumentExpression("_")] string expr = "")
        {
            var (fieldDisplay, code) = ResolveFieldAndCode(codePrefix, propertyName, field, expr);
            return ErrorCatalog.GeneralError.Compare.LessThanOrEqual(fieldDisplay, value, code, isInvariant, message);
        }

        #endregion

        #region Time

        public static Error AfterError<TProperty>(
            this TProperty _,
            string time,
            bool isInvariant = false,
            string? codePrefix = null,
            string propertyName = "",
            string? field = null, 
            string? message = null,
            [CallerArgumentExpression("_")] string expr = "")
        {
            var (fieldDisplay, code) = ResolveFieldAndCode(codePrefix, propertyName, field, expr);
            return ErrorCatalog.GeneralError.Time.After(fieldDisplay, time, code, isInvariant, message);
        }

        public static Error BeforeError<TProperty>(
            this TProperty _,
            string time,
            bool isInvariant = false,
            string? codePrefix = null,
            string propertyName = "",
            string? field = null, 
            string? message = null,
            [CallerArgumentExpression("_")] string expr = "")
        {
            var (fieldDisplay, code) = ResolveFieldAndCode(codePrefix, propertyName, field, expr);
            return ErrorCatalog.GeneralError.Time.Before(fieldDisplay, time, code, isInvariant, message);
        }

        public static Error TimeRangeError<TProperty>(
            this TProperty _,
            string timeStart,
            string timeEnd,
            bool isInvariant = false,
            string? codePrefix = null,
            string propertyName = "",
            string? field = null, 
            string? message = null,
            [CallerArgumentExpression("_")] string expr = "")
        {
            var (fieldDisplay, code) = ResolveFieldAndCode(codePrefix, propertyName, field, expr);
            return ErrorCatalog.GeneralError.Time.TimeRange(fieldDisplay, timeStart, timeEnd, code, isInvariant, message);
        }

        public static Error NotInPastError<TProperty>(
            this TProperty _,
            bool isInvariant = false,
            string? codePrefix = null,
            string propertyName = "",
            string? field = null, 
            string? message = null,
            [CallerArgumentExpression("_")] string expr = "")
        {
            var (fieldDisplay, code) = ResolveFieldAndCode(codePrefix, propertyName, field, expr);
            return ErrorCatalog.GeneralError.Time.NotInPast(fieldDisplay, code, isInvariant, message);
        }
        
        public static Error NotInFutureError<TProperty>(
            this TProperty _,
            bool isInvariant = false,
            string? codePrefix = null,
            string propertyName = "",
            string? field = null, 
            string? message = null,
            [CallerArgumentExpression("_")] string expr = "")
        {
            var (fieldDisplay, code) = ResolveFieldAndCode(codePrefix, propertyName, field, expr);
            return ErrorCatalog.GeneralError.Time.NotInFuture(fieldDisplay, code, isInvariant, message);
        }
        
        public static Error UtcError<TProperty>(
            this TProperty _,
            bool isInvariant = false,
            string? codePrefix = null,
            string propertyName = "",
            string? field = null, 
            string? message = null,
            [CallerArgumentExpression("_")] string expr = "")
        {
            var (fieldDisplay, code) = ResolveFieldAndCode(codePrefix, propertyName, field, expr);
            return ErrorCatalog.GeneralError.Time.Utc(fieldDisplay, code, isInvariant, message);
        }
        
        public static Error NotOverlappingError<TProperty>(
            this TProperty _,
            bool isInvariant = false,
            string? codePrefix = null,
            string propertyName = "",
            string? field = null, 
            string? message = null,
            [CallerArgumentExpression("_")] string expr = "")
        {
            var (fieldDisplay, code) = ResolveFieldAndCode(codePrefix, propertyName, field, expr);
            return ErrorCatalog.GeneralError.Time.NotOverlapping(fieldDisplay, code, isInvariant, message);
        }
        #endregion
    
    extension<T>(T)
    {
        #region Required

        public static Error NotNullError<TProperty>(
            Expression<Func<T, TProperty>> exprPropertyName,
            bool isInvariant = false,
            string? codePrefix = null,
            string? propertyName = null,
            string? field = null, 
            string? message = null)
        {
            var (fieldDisplay, code) = ResolveFieldAndCode(exprPropertyName, codePrefix, propertyName, field);
            return ErrorCatalog.GeneralError.Required.NotNull(fieldDisplay, code, isInvariant, message);
        }
        
        public static Error NotEmptyError<TProperty>(
            Expression<Func<T, TProperty>> exprPropertyName,
            bool isInvariant = false,
            string? codePrefix = null,
            string? propertyName = null,
            string? field = null, 
            string? message = null)
        {
            var (fieldDisplay, code) = ResolveFieldAndCode(exprPropertyName, codePrefix, propertyName, field);
            return ErrorCatalog.GeneralError.Required.NotEmpty(fieldDisplay, code, isInvariant, message);
        }
        
        public static Error NotEmptyGuidError<TProperty>(
            Expression<Func<T, TProperty>> exprPropertyName,
            bool isInvariant = false,
            string? codePrefix = null,
            string? propertyName = null,
            string? field = null, 
            string? message = null)
        {
            var (fieldDisplay, code) = ResolveFieldAndCode(exprPropertyName, codePrefix, propertyName, field);
            return ErrorCatalog.GeneralError.Required.NotEmptyGuid(fieldDisplay, code, isInvariant, message);
        }
        
        public static Error NotDefaultError<TProperty>(
            Expression<Func<T, TProperty>> exprPropertyName,
            T? @default,
            bool isInvariant = false,
            string? codePrefix = null,
            string? propertyName = null,
            string? field = null, 
            string? message = null)
        {
            var (fieldDisplay, code) = ResolveFieldAndCode(exprPropertyName, codePrefix, propertyName, field);
            return ErrorCatalog.GeneralError.Required.NotDefault(fieldDisplay, @default, code, isInvariant, message);
        }
        
        public static Error NotBlankError<TProperty>(
            Expression<Func<T, TProperty>> exprPropertyName,
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

        public static Error MatchesError<TProperty>(
            Expression<Func<T, TProperty>> exprPropertyName,
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
        
        public static Error StartsWithError<TProperty>(
            Expression<Func<T, TProperty>> exprPropertyName,
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
        
        public static Error EndsWithError<TProperty>(
            Expression<Func<T, TProperty>> exprPropertyName,
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
        
        public static Error ContainsError<TProperty>(
            Expression<Func<T, TProperty>> exprPropertyName,
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
        
        public static Error MaxLengthError<TProperty>(
            Expression<Func<T, TProperty>> exprPropertyName,
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
        
        public static Error MinLengthError<TProperty>(
            Expression<Func<T, TProperty>> exprPropertyName,
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
        
        public static Error ExactLengthError<TProperty>(
            Expression<Func<T, TProperty>> exprPropertyName,
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
        
        public static Error LengthBetweenError<TProperty>(
            Expression<Func<T, TProperty>> exprPropertyName,
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
        
        public static Error FormatError<TProperty>(
            Expression<Func<T, TProperty>> exprPropertyName,
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

        public static Error PositiveError<TProperty>(
            Expression<Func<T, TProperty>> exprPropertyName,
            bool isInvariant = false,
            string? codePrefix = null,
            string? propertyName = null,
            string? field = null, 
            string? message = null)
        {
            var (fieldDisplay, code) = ResolveFieldAndCode(exprPropertyName, codePrefix, propertyName, field);
            return ErrorCatalog.GeneralError.Numeric.Positive(fieldDisplay, code, isInvariant, message);
        }

        public static Error NegativeError<TProperty>(
            Expression<Func<T, TProperty>> exprPropertyName,
            bool isInvariant = false,
            string? codePrefix = null,
            string? propertyName = null,
            string? field = null, 
            string? message = null)
        {
            var (fieldDisplay, code) = ResolveFieldAndCode(exprPropertyName, codePrefix, propertyName, field);
            return ErrorCatalog.GeneralError.Numeric.Negative(fieldDisplay, code, isInvariant, message);
        }

        public static Error NonPositiveError<TProperty>(
            Expression<Func<T, TProperty>> exprPropertyName,
            bool isInvariant = false,
            string? codePrefix = null,
            string? propertyName = null,
            string? field = null, 
            string? message = null)
        {
            var (fieldDisplay, code) = ResolveFieldAndCode(exprPropertyName, codePrefix, propertyName, field);
            return ErrorCatalog.GeneralError.Numeric.NonPositive(fieldDisplay, code, isInvariant, message);
        }

        public static Error NonNegativeError<TProperty>(
            Expression<Func<T, TProperty>> exprPropertyName,
            bool isInvariant = false,
            string? codePrefix = null,
            string? propertyName = null,
            string? field = null, 
            string? message = null)
        {
            var (fieldDisplay, code) = ResolveFieldAndCode(exprPropertyName, codePrefix, propertyName, field);
            return ErrorCatalog.GeneralError.Numeric.NonNegative(fieldDisplay, code, isInvariant, message);
        }

        public static Error ZeroError<TProperty>(
            Expression<Func<T, TProperty>> exprPropertyName,
            bool isInvariant = false,
            string? codePrefix = null,
            string? propertyName = null,
            string? field = null, 
            string? message = null)
        {
            var (fieldDisplay, code) = ResolveFieldAndCode(exprPropertyName, codePrefix, propertyName, field);
            return ErrorCatalog.GeneralError.Numeric.Zero(fieldDisplay, code, isInvariant, message);
        }

        public static Error NonZeroError<TProperty>(
            Expression<Func<T, TProperty>> exprPropertyName,
            bool isInvariant = false,
            string? codePrefix = null,
            string? propertyName = null,
            string? field = null, 
            string? message = null)
        {
            var (fieldDisplay, code) = ResolveFieldAndCode(exprPropertyName, codePrefix, propertyName, field);
            return ErrorCatalog.GeneralError.Numeric.NonZero(fieldDisplay, code, isInvariant, message);
        }

        public static Error MultipleOfError<TProperty>(
            Expression<Func<T, TProperty>> exprPropertyName,
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

        public static Error PrecisionScaleError<TProperty>(
            Expression<Func<T, TProperty>> exprPropertyName,
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

        public static Error NotContainsError<TProperty>(
            Expression<Func<T, TProperty>> exprPropertyName,
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

        public static Error NotInSetError<TProperty>(
            Expression<Func<T, TProperty>> exprPropertyName,
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

        public static Error InSetError<TProperty>(
            Expression<Func<T, TProperty>> exprPropertyName,
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

        public static Error NoDuplicatesError<TProperty>(
            Expression<Func<T, TProperty>> exprPropertyName,
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

        public static Error CountMinError<TProperty>(
            Expression<Func<T, TProperty>> exprPropertyName,
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

        public static Error CountMaxError<TProperty>(
            Expression<Func<T, TProperty>> exprPropertyName,
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

        public static Error CountBetweenError<TProperty>(
            Expression<Func<T, TProperty>> exprPropertyName,
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

        public static Error ScalarNotInSetError<TProperty>(
            Expression<Func<T, TProperty>> exprPropertyName,
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
    
        public static Error ScalarInSetError<TProperty>(
            Expression<Func<T, TProperty>> exprPropertyName,
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

        public static Error InEnumError<TProperty>(
            Expression<Func<T, TProperty>> exprPropertyName,
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

        public static Error EqualError<TProperty>(
            Expression<Func<T, TProperty>> exprPropertyName,
            object value,
            bool isInvariant = false,
            string? codePrefix = null,
            string? propertyName = null,
            string? field = null, 
            string? message = null)
        {
            var (fieldDisplay, code) = ResolveFieldAndCode(exprPropertyName, codePrefix, propertyName, field);
            return ErrorCatalog.GeneralError.Compare.Equal(fieldDisplay, value, code, isInvariant, message);
        }

        public static Error NotEqualError<TProperty>(
            Expression<Func<T, TProperty>> exprPropertyName,
            object value,
            bool isInvariant = false,
            string? codePrefix = null,
            string? propertyName = null,
            string? field = null, 
            string? message = null)
        {
            var (fieldDisplay, code) = ResolveFieldAndCode(exprPropertyName, codePrefix, propertyName, field);
            return ErrorCatalog.GeneralError.Compare.NotEqual(fieldDisplay, value, code, isInvariant, message);
        }

        public static Error BetweenInclusiveError<TProperty>(
            Expression<Func<T, TProperty>> exprPropertyName,
            object min,
            object max,
            bool isInvariant = false,
            string? codePrefix = null,
            string? propertyName = null,
            string? field = null, 
            string? message = null)
        {
            var (fieldDisplay, code) = ResolveFieldAndCode(exprPropertyName, codePrefix, propertyName, field);
            return ErrorCatalog.GeneralError.Compare.BetweenInclusive(fieldDisplay, min, max, code, isInvariant, message);
        }

        public static Error BetweenExclusiveError<TProperty>(
            Expression<Func<T, TProperty>> exprPropertyName,
            object min,
            object max,
            bool isInvariant = false,
            string? codePrefix = null,
            string? propertyName = null,
            string? field = null, 
            string? message = null)
        {
            var (fieldDisplay, code) = ResolveFieldAndCode(exprPropertyName, codePrefix, propertyName, field);
            return ErrorCatalog.GeneralError.Compare.BetweenExclusive(fieldDisplay, min, max, code, isInvariant, message);
        }
        
        public static Error GreaterThanError<TProperty>(
            Expression<Func<T, TProperty>> exprPropertyName,
            object value,
            bool isInvariant = false,
            string? codePrefix = null,
            string? propertyName = null,
            string? field = null, 
            string? message = null)
        {
            var (fieldDisplay, code) = ResolveFieldAndCode(exprPropertyName, codePrefix, propertyName, field);
            return ErrorCatalog.GeneralError.Compare.GreaterThan(fieldDisplay, value, code, isInvariant, message);
        }
        
        public static Error GreaterThanOrEqualError<TProperty>(
            Expression<Func<T, TProperty>> exprPropertyName,
            object value,
            bool isInvariant = false,
            string? codePrefix = null,
            string? propertyName = null,
            string? field = null, 
            string? message = null)
        {
            var (fieldDisplay, code) = ResolveFieldAndCode(exprPropertyName, codePrefix, propertyName, field);
            return ErrorCatalog.GeneralError.Compare.GreaterThanOrEqual(fieldDisplay, value, code, isInvariant, message);
        }
        
        public static Error LessThanError<TProperty>(
            Expression<Func<T, TProperty>> exprPropertyName,
            object value,
            bool isInvariant = false,
            string? codePrefix = null,
            string? propertyName = null,
            string? field = null, 
            string? message = null)
        {
            var (fieldDisplay, code) = ResolveFieldAndCode(exprPropertyName, codePrefix, propertyName, field);
            return ErrorCatalog.GeneralError.Compare.LessThan(fieldDisplay, value, code, isInvariant, message);
        }
        
        public static Error LessThanOrEqualError<TProperty>(
            Expression<Func<T, TProperty>> exprPropertyName,
            object value,
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

        #region Date

        public static Error AfterError<TProperty>(
            Expression<Func<T, TProperty>> exprPropertyName,
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

        public static Error BeforeError<TProperty>(
            Expression<Func<T, TProperty>> exprPropertyName,
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

        public static Error TimeRangeError<TProperty>(
            Expression<Func<T, TProperty>> exprPropertyName,
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

        public static Error NotInPastError<TProperty>(
            Expression<Func<T, TProperty>> exprPropertyName,
            bool isInvariant = false,
            string? codePrefix = null,
            string? propertyName = null,
            string? field = null, 
            string? message = null)
        {
            var (fieldDisplay, code) = ResolveFieldAndCode(exprPropertyName, codePrefix, propertyName, field);
            return ErrorCatalog.GeneralError.Time.NotInPast(fieldDisplay, code, isInvariant, message);
        }
        
        public static Error NotInFutureError<TProperty>(
            Expression<Func<T, TProperty>> exprPropertyName,
            bool isInvariant = false,
            string? codePrefix = null,
            string? propertyName = null,
            string? field = null, 
            string? message = null)
        {
            var (fieldDisplay, code) = ResolveFieldAndCode(exprPropertyName, codePrefix, propertyName, field);
            return ErrorCatalog.GeneralError.Time.NotInFuture(fieldDisplay, code, isInvariant, message);
        }
        
        public static Error UtcError<TProperty>(
            Expression<Func<T, TProperty>> exprPropertyName,
            bool isInvariant = false,
            string? codePrefix = null,
            string? propertyName = null,
            string? field = null, 
            string? message = null)
        {
            var (fieldDisplay, code) = ResolveFieldAndCode(exprPropertyName, codePrefix, propertyName, field);
            return ErrorCatalog.GeneralError.Time.Utc(fieldDisplay, code, isInvariant, message);
        }
        
        public static Error NotOverlappingError<TProperty>(
            Expression<Func<T, TProperty>> exprPropertyName,
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