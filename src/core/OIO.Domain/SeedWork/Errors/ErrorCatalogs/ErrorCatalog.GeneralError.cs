using System.Numerics;
using FormatWith;
using OIO.Domain.SeedWork.Utils;

namespace OIO.Domain.SeedWork.Errors.ErrorCatalogs;

public static partial class ErrorCatalog
{
    public static partial class GeneralError
    {
        public static class Required
        {
            public static Error NotNull(
                string field, 
                string codeBase, 
                bool isInvariant = false,
                string? message = null)
            {
                const string rule = Rule.Required.NotNull;
                const string defaultMessage = DefaultTemplate.Required.NotNull;

                var code = string.Join(Constant.DefaultErrorCodeSeparator, codeBase, rule);
                
                message ??= defaultMessage.FormatWith(new Dictionary<string, object?> { ["field"] = field });
                
                return isInvariant ?
                    Error.Invariant(field, code, message) :
                    Error.Validation(field, code, message);
            }
            
            public static Error NotEmpty(
                string field,
                string codeBase,
                bool isInvariant = false,
                string? message = null)
            {
                const string rule = Rule.Required.NotEmpty;
                const string defaultMessage = DefaultTemplate.Required.NotEmpty;
                    
                var code = string.Join(Constant.DefaultErrorCodeSeparator, codeBase, rule);

                message ??= defaultMessage.FormatWith(new Dictionary<string, object?> { ["field"] = field });

                return isInvariant ?
                    Error.Invariant(field, code, message) :
                    Error.Validation(field, code, message);
            }

            public static Error NotEmptyGuid(
                string field,
                string codeBase, 
                bool isInvariant = false,
                string? message = null)
            {
                const string rule = Rule.Required.NotEmptyGuid;
                const string defaultMessage = DefaultTemplate.Required.NotEmptyGuid;
                    
                var code = string.Join(Constant.DefaultErrorCodeSeparator, codeBase, rule);
                 
                message ??= defaultMessage.FormatWith(new Dictionary<string, object?> { ["field"] = field });
                
                return  isInvariant ?
                    Error.Invariant(field, code, message) :
                    Error.Validation(field, code, message);
            }

            public static Error NotDefault<T>(
                string field,
                T? @default,
                string codeBase, 
                bool isInvariant = false,
                string? message = null)
            {
                const string rule = Rule.Required.NotDefault;
                const string defaultMessage = DefaultTemplate.Required.NotDefault;

                var code = string.Join(Constant.DefaultErrorCodeSeparator, codeBase, rule);
                
                message ??= defaultMessage.FormatWith(new Dictionary<string, object?> { ["field"] = field, ["default"] = @default });

                return isInvariant ?
                    Error.Invariant(field, code, message) :
                    Error.Validation(field, code, message);
            }
            
            public static Error NotBlank(
                string field,
                string codeBase, 
                bool isInvariant = false,
                string? message = null)
            {
                const string rule = Rule.Required.NotBlank;
                const string defaultMessage = DefaultTemplate.Required.NotBlank;
                    
                var code = string.Join(Constant.DefaultErrorCodeSeparator, codeBase, rule);               
                
                message ??= defaultMessage.FormatWith(new Dictionary<string, object?> { ["field"] = field });
                
                return isInvariant ?
                    Error.Invariant(field, code, message) :
                    Error.Validation(field, code, message);
            }
        }

        public static class Text
        {
            public static Error Matches(
                string field,
                string pattern,
                string codeBase, 
                bool isInvariant = false,
                string? message = null)
            {
                const string rule = Rule.Text.Matches;
                const string defaultMessage = DefaultTemplate.Text.Matches;
                    
                var code = string.Join(Constant.DefaultErrorCodeSeparator, codeBase, rule);               
                
                message ??= defaultMessage.FormatWith(new Dictionary<string, object?> { ["field"] = field, ["pattern"] = pattern });
                
                return  isInvariant ?
                    Error.Invariant(field, code, message) :
                    Error.Validation(field, code, message);
            }
            
            public static Error StartsWith(
                string field, 
                string prefix, 
                string codeBase, 
                bool isInvariant = false,
                string? message = null)
            {
                const string rule = Rule.Text.StartsWith;
                const string defaultMessage = DefaultTemplate.Text.StartsWith;
                    
                var code = string.Join(Constant.DefaultErrorCodeSeparator, codeBase, rule);               
                
                message ??= defaultMessage.FormatWith(new Dictionary<string, object?> { ["field"] = field, ["prefix"] = prefix });
                return  isInvariant ?
                    Error.Invariant(field, code, message) :
                    Error.Validation(field, code, message);
            }
            
            public static Error EndsWith(
                string field, 
                string suffix, 
                string codeBase, 
                bool isInvariant = false,
                string? message = null)
            {
                const string rule = Rule.Text.EndsWith;
                const string defaultMessage = DefaultTemplate.Text.EndsWith;
                    
                var code = string.Join(Constant.DefaultErrorCodeSeparator, codeBase, rule);               
                
                message ??= defaultMessage.FormatWith(new Dictionary<string, object?> { ["field"] = field, ["suffix"] = suffix });
                return  isInvariant ?
                    Error.Invariant(field, code, message) :
                    Error.Validation(field, code, message);
            }
            
            public static Error Contains(
                string field, 
                string substring, 
                string codeBase, 
                bool isInvariant = false,
                string? message = null)
            {
                const string rule = Rule.Text.Contains;
                const string defaultMessage = DefaultTemplate.Text.Contains;
                    
                var code = string.Join(Constant.DefaultErrorCodeSeparator, codeBase, rule);               
                
                message ??= defaultMessage.FormatWith(new Dictionary<string, object?> { ["field"] = field, ["substring"] = substring });
                return  isInvariant ?
                    Error.Invariant(field, code, message) :
                    Error.Validation(field, code, message);
            }
            
            public static Error MaxLength(
                string field, 
                int max, 
                string codeBase, 
                bool isInvariant = false,
                string? message = null)
            {
                const string rule = Rule.Text.MaxLength;
                const string defaultMessage = DefaultTemplate.Text.MaxLength;
                    
                var code = string.Join(Constant.DefaultErrorCodeSeparator, codeBase, rule);               
                
                message ??= defaultMessage.FormatWith(new Dictionary<string, object?> { ["field"] = field, ["max"] = max });
                return  isInvariant ?
                    Error.Invariant(field, code, message) :
                    Error.Validation(field, code, message);
            }
            
            public static Error MinLength(
                string field,
                int min,
                string codeBase,
                bool isInvariant = false,
                string? message = null)
            {
                const string rule = Rule.Text.MinLength;
                const string defaultMessage = DefaultTemplate.Text.MinLength; 
                    
                var code = string.Join(Constant.DefaultErrorCodeSeparator, codeBase, rule);
                
                message ??= defaultMessage.FormatWith(new Dictionary<string, object?> { ["field"] = field, ["min"] = min });

                return  isInvariant ?
                    Error.Invariant(field, code, message) :
                    Error.Validation(field, code, message);
            }
            
            public static Error ExactLength(
                string field,
                int length,
                string codeBase,
                bool isInvariant = false,
                string? message = null)
            {
                const string rule = Rule.Text.ExactLength;
                const string defaultMessage = DefaultTemplate.Text.ExactLength; 
                    
                var code = string.Join(Constant.DefaultErrorCodeSeparator, codeBase, rule);
                
                message ??= defaultMessage.FormatWith(new Dictionary<string, object?> { ["field"] = field, ["length"] = length });

                return  isInvariant ?
                    Error.Invariant(field, code, message) :
                    Error.Validation(field, code, message);
            }
            
            public static Error LengthBetween(
                string field,
                int min,
                int max,
                string codeBase,
                bool isInvariant = false,
                string? message = null)
            {
                const string rule = Rule.Text.LengthBetween;
                const string defaultMessage = DefaultTemplate.Text.LengthBetween; 
                    
                var code = string.Join(Constant.DefaultErrorCodeSeparator, codeBase, rule);
                
                message ??= defaultMessage.FormatWith(new Dictionary<string, object?> { ["field"] = field, ["min"] = min, ["max"] = max });

                return  isInvariant ?
                    Error.Invariant(field, code, message) :
                    Error.Validation(field, code, message);
            }
            
            public static Error Format(
                string field, 
                string codeBase,
                bool isInvariant = false,
                string? message = null)
            {
                const string rule = Rule.Text.Format;
                const string defaultMessage = DefaultTemplate.Text.Format; 
                    
                var code = string.Join(Constant.DefaultErrorCodeSeparator, codeBase, rule);
                
                message ??= defaultMessage.FormatWith(new Dictionary<string, object?> { ["field"] = field});

                return  isInvariant ?
                    Error.Invariant(field, code, message) :
                    Error.Validation(field, code, message);
            }
        }

        public static class Numeric
        {
            public static Error Positive(
            string field,
            string codeBase,
            bool isInvariant = false,
            string? message = null)
            {
                const string rule = Rule.Numeric.Positive;
                const string defaultMessage = DefaultTemplate.Numeric.Positive; 
                    
                var code = string.Join(Constant.DefaultErrorCodeSeparator, codeBase, rule);
                
                message ??= defaultMessage.FormatWith(new Dictionary<string, object?> { ["field"] = field });

                return  isInvariant ?
                    Error.Invariant(field, code, message) :
                    Error.Validation(field, code, message);
            }

            public static Error Negative(
                string field,
                string codeBase, 
                bool isInvariant = false,
                string? message = null)
            {
                const string rule = Rule.Numeric.Negative;
                const string defaultMessage = DefaultTemplate.Numeric.Negative; 
                    
                var code = string.Join(Constant.DefaultErrorCodeSeparator, codeBase, rule);
                
                message ??= defaultMessage.FormatWith(new Dictionary<string, object?> { ["field"] = field });

                return  isInvariant ?
                    Error.Invariant(field, code, message) :
                    Error.Validation(field, code, message);
            }

            public static Error NonPositive(
                string field,
                string codeBase,
                bool isInvariant = false,
                string? message = null)
            {
                const string rule = Rule.Numeric.NonPositive;
                const string defaultMessage = DefaultTemplate.Numeric.NonPositive; 
                    
                var code = string.Join(Constant.DefaultErrorCodeSeparator, codeBase, rule);
                
                message ??= defaultMessage.FormatWith(new Dictionary<string, object?> { ["field"] = field });

                return  isInvariant ?
                    Error.Invariant(field, code, message) :
                    Error.Validation(field, code, message);
            }

            public static Error NonNegative(
                string field,
                string codeBase,
                bool isInvariant = false,
                string? message = null)
            {
                const string rule = Rule.Numeric.NonNegative;
                const string defaultMessage = DefaultTemplate.Numeric.NonNegative; 
                    
                var code = string.Join(Constant.DefaultErrorCodeSeparator, codeBase, rule);
                
                message ??= defaultMessage.FormatWith(new Dictionary<string, object?> { ["field"] = field });

                return  isInvariant ?
                    Error.Invariant(field, code, message) :
                    Error.Validation(field, code, message);
            }

            public static Error Zero(
                string field,
                string codeBase,
                bool isInvariant = false,
                string? message = null)
            {
                const string rule = Rule.Numeric.Zero;
                const string defaultMessage = DefaultTemplate.Numeric.Zero; 
                    
                var code = string.Join(Constant.DefaultErrorCodeSeparator, codeBase, rule);
                
                message ??= defaultMessage.FormatWith(new Dictionary<string, object?> { ["field"] = field });

                return  isInvariant ?
                    Error.Invariant(field, code, message) :
                    Error.Validation(field, code, message);
            }

            public static Error NonZero(
                string field,
                string codeBase,
                bool isInvariant = false,
                string? message = null)
            {
                const string rule = Rule.Numeric.NonZero;
                const string defaultMessage = DefaultTemplate.Numeric.NonZero; 
                    
                var code = string.Join(Constant.DefaultErrorCodeSeparator, codeBase, rule);
                
                message ??= defaultMessage.FormatWith(new Dictionary<string, object?> { ["field"] = field });

                return  isInvariant ?
                    Error.Invariant(field, code, message) :
                    Error.Validation(field, code, message);
            }

            public static Error MultipleOf<TNumber>(
                string field,
                TNumber step,
                string codeBase,
                bool isInvariant = false,
                string? message = null)  where TNumber : INumber<TNumber>
            {
                const string rule = Rule.Numeric.MultipleOf;
                const string defaultMessage = DefaultTemplate.Numeric.MultipleOf; 
                    
                var code = string.Join(Constant.DefaultErrorCodeSeparator, codeBase, rule);
                
                message ??= defaultMessage.FormatWith(new Dictionary<string, object?> { ["field"] = field, ["step"] = step });

                return  isInvariant ?
                    Error.Invariant(field, code, message) :
                    Error.Validation(field, code, message);
            }
            
            
            public static Error PrecisionScale(
                string field,
                int precision,
                int scale,
                string codeBase, 
                bool isInvariant = false,
                string? message = null)
            {
                const string rule = Rule.Numeric.PrecisionScale;
                const string defaultMessage = DefaultTemplate.Numeric.PrecisionScale; 
                    
                var code = string.Join(Constant.DefaultErrorCodeSeparator, codeBase, rule);
                
                message ??= defaultMessage.FormatWith(new Dictionary<string, object?> { ["field"] = field, ["precision"] = precision, ["scale"] = scale });

                return  isInvariant ?
                    Error.Invariant(field, code, message) :
                    Error.Validation(field, code, message);
            }
        }
        
        public static class Collection
        {
            public static Error NotContains(
                string field,
                string item,
                string codeBase,
                bool isInvariant = false,
                string? message = null)
            {
                const string rule = Rule.Collection.NotContains;
                const string defaultMessage = DefaultTemplate.Collection.NotContains; 
                    
                var code = string.Join(Constant.DefaultErrorCodeSeparator, codeBase, rule);
                
                message ??= defaultMessage.FormatWith(new Dictionary<string, object?> { ["field"] = field, ["item"] = item });

                return  isInvariant ?
                    Error.Invariant(field, code, message) :
                    Error.Validation(field, code, message);
            }

            public static Error NotInSet(
                string field,
                string setString,
                string codeBase, 
                bool isInvariant = false,
                string? message = null)
            {
                const string rule = Rule.Collection.NotInSet;
                const string defaultMessage = DefaultTemplate.Collection.NotInSet; 
                    
                var code = string.Join(Constant.DefaultErrorCodeSeparator, codeBase, rule);
                
                message ??= defaultMessage.FormatWith(new Dictionary<string, object?> { ["field"] = field, ["set"] = setString });

                return  isInvariant ?
                    Error.Invariant(field, code, message) :
                    Error.Validation(field, code, message);
            }

            public static Error InSet(
                string field,
                string setString,
                string codeBase, 
                bool isInvariant = false,
                string? message = null)
            {
                const string rule = Rule.Collection.InSet;
                const string defaultMessage = DefaultTemplate.Collection.InSet; 
                    
                var code = string.Join(Constant.DefaultErrorCodeSeparator, codeBase, rule);
                
                message ??= defaultMessage.FormatWith(new Dictionary<string, object?> { ["field"] = field, ["set"] = setString });

                return  isInvariant ?
                    Error.Invariant(field, code, message) :
                    Error.Validation(field, code, message);
            }

            public static Error NoDuplicates(
                string field,
                string duplicate,
                string codeBase,
                bool isInvariant = false,
                string? message = null)
            {
                const string rule = Rule.Collection.NoDuplicates;
                const string defaultMessage = DefaultTemplate.Collection.NoDuplicates; 
                    
                var code = string.Join(Constant.DefaultErrorCodeSeparator, codeBase, rule);
                
                message ??= defaultMessage.FormatWith(new Dictionary<string, object?> { ["field"] = field, ["duplicate"] = duplicate });

                return  isInvariant ?
                    Error.Invariant(field, code, message) :
                    Error.Validation(field, code, message);
            }

            public static Error CountMin(
                string field,
                int min,
                string codeBase,
                bool isInvariant = false,
                string? message = null)
            {
                const string rule = Rule.Collection.CountMin;
                const string defaultMessage = DefaultTemplate.Collection.CountMin; 
                    
                var code = string.Join(Constant.DefaultErrorCodeSeparator, codeBase, rule);
                
                message ??= defaultMessage.FormatWith(new Dictionary<string, object?> { ["field"] = field, ["min"] = min });

                return  isInvariant ?
                    Error.Invariant(field, code, message) :
                    Error.Validation(field, code, message);
            }

            public static Error CountMax(
                string field,
                int max,
                string codeBase,
                bool isInvariant = false,
                string? message = null)
            {
                const string rule = Rule.Collection.CountMax;
                const string defaultMessage = DefaultTemplate.Collection.CountMax; 
                    
                var code = string.Join(Constant.DefaultErrorCodeSeparator, codeBase, rule);
                
                message ??= defaultMessage.FormatWith(new Dictionary<string, object?> { ["field"] = field, ["max"] = max });

                return  isInvariant ?
                    Error.Invariant(field, code, message) :
                    Error.Validation(field, code, message);
            }

            public static Error CountBetween(
                string field,
                int min,
                int max,
                string codeBase,
                bool isInvariant = false,
                string? message = null)
            {
                const string rule = Rule.Collection.CountBetween;
                const string defaultMessage = DefaultTemplate.Collection.CountBetween; 
                    
                var code = string.Join(Constant.DefaultErrorCodeSeparator, codeBase, rule);
                
                message ??= defaultMessage.FormatWith(new Dictionary<string, object?> { ["field"] = field, ["min"] = min, ["max"] = max });

                return  isInvariant ?
                    Error.Invariant(field, code, message) :
                    Error.Validation(field, code, message);
            }
        }
        
        public static class Scalar
        {
            public static Error InSet(
                string field,
                string setString,
                string codeBase,
                bool isInvariant = false,
                string? message = null)
            {
                const string rule = Rule.Scalar.InSet;
                const string defaultMessage = DefaultTemplate.Scalar.InSet;

                var code = string.Join(Constant.DefaultErrorCodeSeparator, codeBase, rule);

                message ??= defaultMessage.FormatWith(new Dictionary<string, object?> { ["field"] = field, ["set"] = setString });

                return isInvariant
                    ? Error.Invariant(field, code, message)
                    : Error.Validation(field, code, message);
            }

            public static Error NotInSet(
                string field,
                string setString,
                string codeBase,
                bool isInvariant = false,
                string? message = null)
            {
                const string rule = Rule.Scalar.NotInSet;
                const string defaultMessage = DefaultTemplate.Scalar.NotInSet;

                var code = string.Join(Constant.DefaultErrorCodeSeparator, codeBase, rule);

                message ??= defaultMessage.FormatWith(new Dictionary<string, object?> { ["field"] = field, ["set"] = setString });

                return isInvariant
                    ? Error.Invariant(field, code, message)
                    : Error.Validation(field, code, message);
            }
        }

        public static class Enum
        {
            public static Error InEnum(
                string field,
                string enumName,
                string value,
                string codeBase,
                bool isInvariant = false,
                string? message = null)
            {
                const string rule = Rule.Enum.InEnum;
                const string defaultMessage = DefaultTemplate.Enum.InEnum;

                var code = string.Join(Constant.DefaultErrorCodeSeparator, codeBase, rule);

                message ??= defaultMessage.FormatWith(new Dictionary<string, object?> { ["field"] = field, ["enum"] = enumName, ["value"] = value });

                return isInvariant
                    ? Error.Invariant(field, code, message)
                    : Error.Validation(field, code, message);
            }
        }
        
        public static class Compare
        {
            public static Error Equal<TProperty>(
                string field,
                TProperty value,
                string codeBase,
                bool isInvariant = false,
                string? message = null)
            {
                const string rule = Rule.Compare.Equal;
                const string defaultMessage = DefaultTemplate.Compare.Equal; 
                    
                var code = string.Join(Constant.DefaultErrorCodeSeparator, codeBase, rule);
                
                message ??= defaultMessage.FormatWith(new Dictionary<string, object?> { ["field"] = field, ["value"] = value });

                return  isInvariant ?
                    Error.Invariant(field, code, message) :
                    Error.Validation(field, code, message);
            }

            public static Error NotEqual<TProperty>(
                string field,
                TProperty value,
                string codeBase,
                bool isInvariant = false,
                string? message = null)
            {
                const string rule = Rule.Compare.NotEqual;
                const string defaultMessage = DefaultTemplate.Compare.NotEqual; 
                    
                var code = string.Join(Constant.DefaultErrorCodeSeparator, codeBase, rule);
                
                message ??= defaultMessage.FormatWith(new Dictionary<string, object?> { ["field"] = field, ["value"] = value });

                return  isInvariant ?
                    Error.Invariant(field, code, message) :
                    Error.Validation(field, code, message);
            }
            
            public static Error BetweenInclusive<TProperty>(
                string field,
                TProperty min,
                TProperty max,
                string codeBase,
                bool isInvariant = false,
                string? message = null)
            {
                const string rule = Rule.Compare.BetweenInclusive;
                const string defaultMessage = DefaultTemplate.Compare.BetweenInclusive; 
                    
                var code = string.Join(Constant.DefaultErrorCodeSeparator, codeBase, rule);
                
                message ??= defaultMessage.FormatWith(new Dictionary<string, object?> { ["field"] = field, ["min"] = min, ["max"] = max });

                return  isInvariant ?
                    Error.Invariant(field, code, message) :
                    Error.Validation(field, code, message);
            }

            public static Error BetweenExclusive<TProperty>(
                string field,
                TProperty min,
                TProperty max,
                string codeBase,
                bool isInvariant = false,
                string? message = null)
            {
                const string rule = Rule.Compare.BetweenExclusive;
                const string defaultMessage = DefaultTemplate.Compare.BetweenExclusive; 
                    
                var code = string.Join(Constant.DefaultErrorCodeSeparator, codeBase, rule);
                
                message ??= defaultMessage.FormatWith(new Dictionary<string, object?> { ["field"] = field, ["min"] = min, ["max"] = max });

                return  isInvariant ?
                    Error.Invariant(field, code, message) :
                    Error.Validation(field, code, message);
            }

            public static Error GreaterThan<TProperty>(
                string field,
                TProperty value,
                string codeBase,
                bool isInvariant = false,
                string? message = null)
            {
                const string rule = Rule.Compare.GreaterThan;
                const string defaultMessage = DefaultTemplate.Compare.GreaterThan; 
                    
                var code = string.Join(Constant.DefaultErrorCodeSeparator, codeBase, rule);
                
                message ??= defaultMessage.FormatWith(new Dictionary<string, object?> { ["field"] = field, ["value"] = value });

                return  isInvariant ?
                    Error.Invariant(field, code, message) :
                    Error.Validation(field, code, message);
            }

            public static Error GreaterThanOrEqual<TProperty>(
                string field,
                TProperty value,
                string codeBase,
                bool isInvariant = false,
                string? message = null)
            {
                const string rule = Rule.Compare.GreaterThanOrEqual;
                const string defaultMessage = DefaultTemplate.Compare.GreaterThanOrEqual; 
                    
                var code = string.Join(Constant.DefaultErrorCodeSeparator, codeBase, rule);
                
                message ??= defaultMessage.FormatWith(new Dictionary<string, object?> { ["field"] = field, ["value"] = value });

                return  isInvariant ?
                    Error.Invariant(field, code, message) :
                    Error.Validation(field, code, message);
            }

            public static Error LessThan<TProperty>(
                string field,
                TProperty value,
                string codeBase,
                bool isInvariant = false,
                string? message = null)
            {
                const string rule = Rule.Compare.LessThan;
                const string defaultMessage = DefaultTemplate.Compare.LessThan; 
                    
                var code = string.Join(Constant.DefaultErrorCodeSeparator, codeBase, rule);
                
                message ??= defaultMessage.FormatWith(new Dictionary<string, object?> { ["field"] = field, ["value"] = value });

                return  isInvariant ?
                    Error.Invariant(field, code, message) :
                    Error.Validation(field, code, message);
            }

            public static Error LessThanOrEqual<TProperty>(
                string field,
                TProperty value,
                string codeBase,
                bool isInvariant = false,
                string? message = null)
            {
                const string rule = Rule.Compare.LessThanOrEqual;
                const string defaultMessage = DefaultTemplate.Compare.LessThanOrEqual;

                var code = string.Join(Constant.DefaultErrorCodeSeparator, codeBase, rule);
                
                message ??= defaultMessage.FormatWith(new Dictionary<string, object?> { ["field"] = field, ["value"] = value });

                return  isInvariant ?
                    Error.Invariant(field, code, message) :
                    Error.Validation(field, code, message);
            }
        }
        
        public static class Time
        {
            public static Error After(
                string field,
                string time,
                string codeBase,
                bool isInvariant = false,
                string? message = null)
            {
                const string rule = Rule.Time.After;
                const string defaultMessage = DefaultTemplate.Time.After;

                var code = string.Join(Constant.DefaultErrorCodeSeparator, codeBase, rule);
                
                message ??= defaultMessage.FormatWith(new Dictionary<string, object?> { ["field"] = field, ["time"] = time });

                return  isInvariant ?
                    Error.Invariant(field, code, message) :
                    Error.Validation(field, code, message);
            }

            public static Error Before(
                string field,
                string time,
                string codeBase,
                bool isInvariant = false,
                string? message = null)
            {
                const string rule = Rule.Time.Before;
                const string defaultMessage = DefaultTemplate.Time.Before;

                var code = string.Join(Constant.DefaultErrorCodeSeparator, codeBase, rule);
                
                message ??= defaultMessage.FormatWith(new Dictionary<string, object?> { ["field"] = field, ["time"] = time });

                return  isInvariant ?
                    Error.Invariant(field, code, message) :
                    Error.Validation(field, code, message);
            }

            public static Error TimeRange(
                string field,
                string timeStart,
                string timeEnd,
                string codeBase,
                bool isInvariant = false,
                string? message = null)
            {
                const string rule = Rule.Time.TimeRange;
                const string defaultMessage = DefaultTemplate.Time.TimeRange;

                var code = string.Join(Constant.DefaultErrorCodeSeparator, codeBase, rule);
                
                message ??= defaultMessage.FormatWith(new Dictionary<string, object?> { ["field"] = field, ["start"] = timeStart, ["end"] = timeEnd });

                return  isInvariant ?
                    Error.Invariant(field, code, message) :
                    Error.Validation(field, code, message);
            }

            public static Error NotInPast(
                string field,
                string codeBase,
                bool isInvariant = false,
                string? message = null)
            {
                const string rule = Rule.Time.NotInPast;
                const string defaultMessage = DefaultTemplate.Time.NotInPast;

                var code = string.Join(Constant.DefaultErrorCodeSeparator, codeBase, rule);
                
                message ??= defaultMessage.FormatWith(new Dictionary<string, object?> { ["field"] = field });

                return  isInvariant ?
                    Error.Invariant(field, code, message) :
                    Error.Validation(field, code, message);
            }

            public static Error NotInFuture(
                string field,
                string codeBase,
                bool isInvariant = false,
                string? message = null)
            {
                const string rule = Rule.Time.NotInFuture;
                const string defaultMessage = DefaultTemplate.Time.NotInFuture;

                var code = string.Join(Constant.DefaultErrorCodeSeparator, codeBase, rule);
                
                message ??= defaultMessage.FormatWith(new Dictionary<string, object?> { ["field"] = field });

                return  isInvariant ?
                    Error.Invariant(field, code, message) :
                    Error.Validation(field, code, message);
            }

            public static Error Utc(
                string field,
                string codeBase,
                bool isInvariant = false,
                string? message = null)
            {
                const string rule = Rule.Time.Utc;
                const string defaultMessage = DefaultTemplate.Time.Utc;

                var code = string.Join(Constant.DefaultErrorCodeSeparator, codeBase, rule);
                
                message ??= defaultMessage.FormatWith(new Dictionary<string, object?> { ["field"] = field });

                return  isInvariant ?
                    Error.Invariant(field, code, message) :
                    Error.Validation(field, code, message);
            }

            public static Error NotOverlapping(
                string field,
                string codeBase,
                bool isInvariant = false,
                string? message = null)
            {
                const string rule = Rule.Time.NotOverlapping;
                const string defaultMessage = DefaultTemplate.Time.NotOverlapping;

                var code = string.Join(Constant.DefaultErrorCodeSeparator, codeBase, rule);
                
                message ??= defaultMessage.FormatWith(new Dictionary<string, object?> { ["field"] = field });

                return isInvariant ?
                    Error.Invariant(field, code, message) :
                    Error.Validation(field, code, message);
            }
        }
    }
}