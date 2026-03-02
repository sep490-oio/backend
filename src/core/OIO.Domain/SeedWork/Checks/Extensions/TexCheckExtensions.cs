using System.Collections.Concurrent;
using System.Text.RegularExpressions;
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
                return error ?? Error.Matches(
                    property: check.Property,
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
            if (!check.ShouldContinue || check.Property.StartsWith(prefix, comparison))
                return check;

            var err = error ?? Error.StartsWith(
                property: check.Property,
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
            if (!check.ShouldContinue || check.Property.EndsWith(suffix, comparison))
                return check;

            var err = error ?? Error.EndsWith(
                property: check.Property,
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
            if (!check.ShouldContinue || check.Property.Contains(substring, comparison))
                return check;

            var err = error ?? Error.Contains(
                property: check.Property,
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
            if (!check.ShouldContinue || check.Property.Length <= max)
                return check;

            var err = error ?? Error.MaxLength(
                property: check.Property,
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
            if (!check.ShouldContinue || check.Property.Length >= min)
                return check;

            var err = error ?? Error.MinLength(
                property: check.Property,
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
            if (!check.ShouldContinue || check.Property.Length == length)
                return check;

            var err = error ?? Error.ExactLength(
                property: check.Property,
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
            if (!check.ShouldContinue || check.Property.Length >= min && check.Property.Length <= max)
                return check;

            var err = error ?? Error.LengthBetween(
                property: check.Property,
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
            if (!check.ShouldContinue || validators.All(v => v(check.Property)))
                return check;

            var err = error ?? Error.Format(
                property: check.Property,
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