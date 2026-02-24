using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using FluentCheck.Errors;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Domain.SeedWork.Checks.Extensions;

public static class TextNullableCheckExtensions
{
    
    extension<TOwner>(CheckField<TOwner, string> check)
    {
        public CheckField<TOwner, string> Matches(
            string pattern,
            RegexOptions options = RegexOptions.None,
            string? message = null,
            Error? error = null
        )
        {
            if (!check.ShouldContinue)
                return check;

            // safety guard (in case runtime null slips in)
            var value = check.Property ?? string.Empty;
        
            try
            {
                var regex = RegexCache.Get(pattern, options);

                var ok = regex.IsMatch(value);

                return ok ? check : check.Fail(CreateError());
            }
            catch (ArgumentException)
            {
                return check.Fail(CreateError());
            }
            catch (RegexMatchTimeoutException)
            {
                return check.Fail(CreateError());
            }

            Error CreateError()
            {
                return error ?? value.MatchesError(
                    pattern: pattern,
                    isInvariant: check.IsInvariant,
                    codePrefix: check.OwnerName,
                    propertyName: check.PropertyName,
                    field: check.FieldName,
                    message: message);
            }
        }

        public CheckField<TOwner, string> StartsWith(
            string prefix,
            StringComparison comparison = StringComparison.Ordinal,
            string? message = null,
            Error? error = null)
        {
            if (!check.ShouldContinue)
                return check;

            var value = check.Property ?? string.Empty;
        
            if (value.StartsWith(prefix, comparison)) 
                return check;

            var err = error ?? value.StartsWithError(
                prefix: prefix,
                isInvariant: check.IsInvariant,
                codePrefix: check.OwnerName,
                propertyName: check.PropertyName,
                field: check.FieldName,
                message: message);

            return check.Fail(err);
        }

        public CheckField<TOwner, string> EndsWith(
            string suffix,
            StringComparison comparison = StringComparison.Ordinal,
            string? message = null,
            Error? error = null)
        {
            if (!check.ShouldContinue)
                return check;

            var value = check.Property ?? string.Empty;
        
            if (value.EndsWith(suffix, comparison)) 
                return check;

            var err = error ?? value.EndsWithError(
                suffix: suffix,
                isInvariant: check.IsInvariant,
                codePrefix: check.OwnerName,
                propertyName: check.PropertyName,
                field: check.FieldName,
                message: message);

            return check.Fail(err);
        }

        public CheckField<TOwner, string> Contains(
            string substring,
            StringComparison comparison = StringComparison.Ordinal,
            string? message = null,
            Error? error = null)
        {
            if (!check.ShouldContinue)
                return check;

            var value = check.Property ?? string.Empty;
        
            if (value.Contains(substring, comparison))
                return check;

            var err = error ?? value.ContainsError(
                substring: substring,
                isInvariant: check.IsInvariant,
                codePrefix: check.OwnerName,
                propertyName: check.PropertyName,
                field: check.FieldName,
                message: message);

            return check.Fail(err);
        }

        public CheckField<TOwner, string> MaxLength(
            int max,
            string? message = null,
            Error? error = null)
        {
            if (!check.ShouldContinue)
                return check;

            var value = check.Property ?? string.Empty;
        
            if (value.Length <= max)
                return check;

            var err = error ?? value.MaxLengthError(
                max: max,
                isInvariant: check.IsInvariant,
                codePrefix: check.OwnerName,
                propertyName: check.PropertyName,
                field: check.FieldName,
                message: message);

            return check.Fail(err);
        }

        public CheckField<TOwner, string> MinLength(
            int min,
            string? message = null,
            Error? error = null)
        {
            if (!check.ShouldContinue)
                return check;

            var value = check.Property ?? string.Empty;
        
            if (value.Length >= min)
                return check;

            var err = error ?? value.MinLengthError(
                min: min,
                isInvariant: check.IsInvariant,
                codePrefix: check.OwnerName,
                propertyName: check.PropertyName,
                field: check.FieldName,
                message: message);

            return check.Fail(err);
        }

        public CheckField<TOwner, string> ExactLength(
            int length,
            string? message = null,
            Error? error = null)
        {
            if (!check.ShouldContinue)
                return check;

            var value = check.Property ?? string.Empty;
        
            if (value.Length == length)
                return check;

            var err = error ?? value.ExactLengthError(
                length: length,
                isInvariant: check.IsInvariant,
                codePrefix: check.OwnerName,
                propertyName: check.PropertyName,
                field: check.FieldName,
                message: message);

            return check.Fail(err);
        }

        public CheckField<TOwner, string> LengthBetween(
            int min,
            int max,
            string? message = null,
            Error? error = null)
        {
            if (!check.ShouldContinue)
                return check;

            var value = check.Property ?? string.Empty;
        
            var len = value.Length;
        
            if (len >= min && len <= max) 
                return check;

            var err = error ?? value.LengthBetweenError(
                min: min,
                max: max,
                isInvariant: check.IsInvariant,
                codePrefix: check.OwnerName,
                propertyName: check.PropertyName,
                field: check.FieldName,
                message: message);

            return check.Fail(err);
        }

        /// <summary>
        /// Generic format rule: truyền predicate để tự check format (email/phone/slug...).
        /// Fail => FormatError(formatMessage).
        /// </summary>
        public CheckField<TOwner, string> Format(
            Func<string, bool> isValid,
            string? message = null,
            Error? error = null)
        {
            return check.Format(message, error, isValid);
        }

        public CheckField<TOwner, string> Format(
            string? message = null,
            Error? error = null,
            params Func<string, bool>[] validators)
        {
            if (!check.ShouldContinue)
                return check;

            var value = check.Property ?? string.Empty;

            if (validators.All(v => v(value)))
                return check;
        
            var err = error ?? value.FormatError(
                isInvariant: check.IsInvariant,
                codePrefix: check.OwnerName,
                propertyName: check.PropertyName,
                field: check.FieldName,
                message: message);

            return check.Fail(err);
        }

        public CheckField<TOwner, string> Format(
            Error? error = null,
            params Func<string, bool>[] validators)
            => check.Format(message: null, error, validators);
        
        public CheckField<TOwner, string> Format(
            string message,
            params Func<string, bool>[] validators)
            => check.Format(message: message, null, validators);
    }

    extension<TOwner>(CheckField<TOwner, string?> check)
    {
        public CheckField<TOwner, string?> MatchesIfNotNull(
            string pattern,
            RegexOptions options = RegexOptions.None,
            string? message = null,
            Error? error = null)
        {
            if (!check.ShouldContinue || check.Property is null)
                return check;

            try
            {
                var regex = RegexCache.Get(pattern, options);

                var ok = regex.IsMatch(check.Property);

                return ok ? check : check.Fail(CreateError());
            }
            catch (ArgumentException)
            {
                return check.Fail(CreateError());
            }
            catch (RegexMatchTimeoutException)
            {
                return check.Fail(CreateError());
            }
            
            Error CreateError()
            {
                return error ?? check.Property.MatchesError(
                    pattern: pattern,
                    isInvariant: check.IsInvariant,
                    codePrefix: check.OwnerName,
                    propertyName: check.PropertyName,
                    field: check.FieldName,
                    message: message);
            }
        }
        
        public CheckField<TOwner, string?> StartsWithIfNotNull(
            string prefix,
            StringComparison comparison = StringComparison.Ordinal,
            string? message = null,
            Error? error = null)
        {
            if (!check.ShouldContinue || check.Property is null || check.Property.StartsWith(prefix, comparison))
                return check;

            var err = error ?? check.Property.StartsWithError(
                prefix: prefix,
                isInvariant: check.IsInvariant,
                codePrefix: check.OwnerName,
                propertyName: check.PropertyName,
                field: check.FieldName,
                message: message);

            return check.Fail(err);
        }
        
        public CheckField<TOwner, string?> EndsWithIfNotNull(
            string suffix,
            StringComparison comparison = StringComparison.Ordinal,
            string? message = null,
            Error? error = null)
        {
            if (!check.ShouldContinue || check.Property is null || check.Property.EndsWith(suffix, comparison))
                return check;

            var err = error ?? check.Property.EndsWithError(
                suffix: suffix,
                isInvariant: check.IsInvariant,
                codePrefix: check.OwnerName,
                propertyName: check.PropertyName,
                field: check.FieldName,
                message: message);

            return check.Fail(err);
        }
        
        public CheckField<TOwner, string?> ContainsIfNotNull(
            string substring,
            StringComparison comparison = StringComparison.Ordinal,
            string? message = null,
            Error? error = null)
        {
            if (!check.ShouldContinue || check.Property is null || check.Property.Contains(substring, comparison))
                return check;

            var err = error ?? check.Property.ContainsError(
                substring: substring,
                isInvariant: check.IsInvariant,
                codePrefix: check.OwnerName,
                propertyName: check.PropertyName,
                field: check.FieldName,
                message: message);

            return check.Fail(err);
        }
        
        public CheckField<TOwner, string?> MaxLengthIfNotNull(
            int max,
            string? message = null,
            Error? error = null)
        {
            if (!check.ShouldContinue || check.Property is null || check.Property.Length <= max)
                return check;

            var err = error ?? check.Property.MaxLengthError(
                max: max,
                isInvariant: check.IsInvariant,
                codePrefix: check.OwnerName,
                propertyName: check.PropertyName,
                field: check.FieldName,
                message: message);

            return check.Fail(err);
        }
        
        public CheckField<TOwner, string?> MinLengthIfNotNull(
            int min,
            string? message = null,
            Error? error = null)
        {
            if (!check.ShouldContinue || check.Property is null || check.Property.Length >= min)
                return check;

            var err = error ?? check.Property.MinLengthError(
                min: min,
                isInvariant: check.IsInvariant,
                codePrefix: check.OwnerName,
                propertyName: check.PropertyName,
                field: check.FieldName,
                message: message);

            return check.Fail(err);
        }
        
        public CheckField<TOwner, string?> ExactLengthIfNotNull(
            int length,
            string? message = null,
            Error? error = null)
        {
            if (!check.ShouldContinue || check.Property is null || check.Property.Length == length)
                return check;

            var err = error ?? check.Property.ExactLengthError(
                length: length,
                isInvariant: check.IsInvariant,
                codePrefix: check.OwnerName,
                propertyName: check.PropertyName,
                field: check.FieldName,
                message: message);

            return check.Fail(err);
        }
        
        public CheckField<TOwner, string?> LengthBetweenIfNotNull(
            int min,
            int max,
            string? message = null,
            Error? error = null)
        {
            if (!check.ShouldContinue || check.Property is null)
                return check;

            var len = check.Property.Length;
            
            if (len >= min && len <= max)
                return check;

            var err = error ?? check.Property.LengthBetweenError(
                min: min,
                max: max,
                isInvariant: check.IsInvariant,
                codePrefix: check.OwnerName,
                propertyName: check.PropertyName,
                field: check.FieldName,
                message: message);

            return check.Fail(err);
        }
        
        public CheckField<TOwner, string?> FormatIfNotNull(
            Func<string, bool> isValid,
            string? message = null,
            Error? error = null)
        {
            return check.FormatIfNotNull(message, error, isValid);
        }
        
        public CheckField<TOwner, string?> FormatIfNotNull(
            string? message = null,
            Error? error = null,
            params Func<string, bool>[] validators)
        {
            if (!check.ShouldContinue || check.Property is null)
                return check;

            var value = check.Property ?? string.Empty;

            if (validators.All(v => v(value)))
                return check;
        
            var err = error ?? value.FormatError(
                isInvariant: check.IsInvariant,
                codePrefix: check.OwnerName,
                propertyName: check.PropertyName,
                field: check.FieldName,
                message: message);

            return check.Fail(err);
        }

        public CheckField<TOwner, string?> FormatIfNotNull(
            Error? error = null,
            params Func<string, bool>[] validators)
            => check.FormatIfNotNull(message: null, error, validators);
        
        public CheckField<TOwner, string?> FormatIfNotNull(
            params Func<string, bool>[] validators)
            => check.FormatIfNotNull(message: null, null, validators);
    }
}

internal static class RegexCache
{
    private static readonly ConcurrentDictionary<Key, Regex> Cache = new();

    // bạn có thể chỉnh timeout theo nhu cầu
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromMilliseconds(250);

    internal static Regex Get(
        string pattern,
        RegexOptions options = RegexOptions.None,
        TimeSpan? timeout = null)
    {
        if (string.IsNullOrEmpty(pattern))
            throw new ArgumentException("pattern must not be null/empty.", nameof(pattern));

        var key = new Key(pattern, options, timeout ?? DefaultTimeout);

        return Cache.GetOrAdd(key, static k =>
            new Regex(k.Pattern, k.Options, k.Timeout));
    }

    // Key nên là record struct để nhanh và có hashing ổn
    private readonly record struct Key(string Pattern, RegexOptions Options, TimeSpan Timeout);
}