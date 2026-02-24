using FluentCheck.Errors;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Domain.SeedWork.Checks.Extensions;

public static class CompareCheckFieldExtensions
{
    // ---------------------------
    // Equal / NotEqual (no constraint needed)
    // ---------------------------

    extension<TOwner, T>(CheckField<TOwner, T> check)
    {
        public CheckField<TOwner, T> EqualTo(T value,
            string? message = null,
            Error? error = null,
            IEqualityComparer<T>? comparer = null)
        {
            if (!check.ShouldContinue) return check;

            comparer ??= EqualityComparer<T>.Default;
            if (comparer.Equals(check.Property, value)) return check;

            var err = error ?? check.Property.EqualError(
                value: value,
                isInvariant: check.IsInvariant,
                codePrefix: check.OwnerName,
                propertyName: check.PropertyName,
                field: check.FieldName,
                message: message);

            return check.Fail(err);
        }

        public CheckField<TOwner, T> NotEqualTo(T value,
            string? message = null,
            Error? error = null,
            IEqualityComparer<T>? comparer = null)
        {
            if (!check.ShouldContinue) return check;

            comparer ??= EqualityComparer<T>.Default;
            if (!comparer.Equals(check.Property, value)) return check;

            var err = error ?? check.Property.NotEqualError(
                value: value,
                isInvariant: check.IsInvariant,
                codePrefix: check.OwnerName,
                propertyName: check.PropertyName,
                field: check.FieldName,
                message: message);

            return check.Fail(err);
        }
    }

    // Nullable structs: only validate when has value
    extension<TOwner, T>(CheckField<TOwner, T?> check) where T : struct
    {
        public CheckField<TOwner, T?> EqualToIfHasValue(T value,
            string? message = null,
            Error? error = null,
            IEqualityComparer<T>? comparer = null)
        {
            if (!check.ShouldContinue || !check.Property.HasValue) return check;

            comparer ??= EqualityComparer<T>.Default;
            if (comparer.Equals(check.Property.Value, value)) return check;

            var err = error ?? check.Property.Value.EqualError(
                value: value,
                isInvariant: check.IsInvariant,
                codePrefix: check.OwnerName,
                propertyName: check.PropertyName,
                field: check.FieldName,
                message: message);

            return check.Fail(err);
        }

        public CheckField<TOwner, T?> NotEqualToIfHasValue(T value,
            string? message = null,
            Error? error = null,
            IEqualityComparer<T>? comparer = null)
        {
            if (!check.ShouldContinue || !check.Property.HasValue) return check;

            comparer ??= EqualityComparer<T>.Default;
            if (!comparer.Equals(check.Property.Value, value)) return check;

            var err = error ?? check.Property.Value.NotEqualError(
                value: value,
                isInvariant: check.IsInvariant,
                codePrefix: check.OwnerName,
                propertyName: check.PropertyName,
                field: check.FieldName,
                message: message);

            return check.Fail(err);
        }
    }

    // ---------------------------
    // Comparable checks
    // ---------------------------

    extension<TOwner, T>(CheckField<TOwner, T> check) where T : IComparable<T>
    {
        public CheckField<TOwner, T> BetweenInclusive(T min,
            T max,
            string? message = null,
            Error? error = null,
            IComparer<T>? comparer = null)
        {
            if (!check.ShouldContinue) return check;

            comparer ??= Comparer<T>.Default;
            var v = check.Property;

            if (comparer.Compare(v, min) >= 0 && comparer.Compare(v, max) <= 0)
                return check;

            var err = error ?? v.BetweenInclusiveError(
                min: min,
                max: max,
                isInvariant: check.IsInvariant,
                codePrefix: check.OwnerName,
                propertyName: check.PropertyName,
                field: check.FieldName,
                message: message);

            return check.Fail(err);
        }

        public CheckField<TOwner, T> BetweenExclusive(T min,
            T max,
            string? message = null,
            Error? error = null,
            IComparer<T>? comparer = null)
        {
            if (!check.ShouldContinue) return check;

            comparer ??= Comparer<T>.Default;
            var v = check.Property;

            if (comparer.Compare(v, min) > 0 && comparer.Compare(v, max) < 0)
                return check;

            var err = error ?? v.BetweenExclusiveError(
                min: min,
                max: max,
                isInvariant: check.IsInvariant,
                codePrefix: check.OwnerName,
                propertyName: check.PropertyName,
                field: check.FieldName,
                message: message);

            return check.Fail(err);
        }

        public CheckField<TOwner, T> GreaterThan(T value,
            string? message = null,
            Error? error = null,
            IComparer<T>? comparer = null)
        {
            if (!check.ShouldContinue) return check;

            comparer ??= Comparer<T>.Default;
            if (comparer.Compare(check.Property, value) > 0) return check;

            var err = error ?? check.Property.GreaterThanError(
                value: value,
                isInvariant: check.IsInvariant,
                codePrefix: check.OwnerName,
                propertyName: check.PropertyName,
                field: check.FieldName,
                message: message);

            return check.Fail(err);
        }

        public CheckField<TOwner, T> GreaterThanOrEqual(T value,
            string? message = null,
            Error? error = null,
            IComparer<T>? comparer = null)
        {
            if (!check.ShouldContinue) 
                return check;

            comparer ??= Comparer<T>.Default;
            
            if (comparer.Compare(check.Property, value) >= 0) 
                return check;

            var err = error ?? check.Property.GreaterThanOrEqualError(
                value: value,
                isInvariant: check.IsInvariant,
                codePrefix: check.OwnerName,
                propertyName: check.PropertyName,
                field: check.FieldName,
                message: message);

            return check.Fail(err);
        }

        public CheckField<TOwner, T> LessThan(T value,
            string? message = null,
            Error? error = null,
            IComparer<T>? comparer = null)
        {
            if (!check.ShouldContinue) return check;

            comparer ??= Comparer<T>.Default;
            
            if (comparer.Compare(check.Property, value) < 0) 
                return check;

            var err = error ?? check.Property.LessThanError(
                value: value,
                isInvariant: check.IsInvariant,
                codePrefix: check.OwnerName,
                propertyName: check.PropertyName,
                field: check.FieldName,
                message: message);

            return check.Fail(err);
        }

        public CheckField<TOwner, T> LessThanOrEqual(T value,
            string? message = null,
            Error? error = null,
            IComparer<T>? comparer = null)
        {
            if (!check.ShouldContinue) return check;

            comparer ??= Comparer<T>.Default;
            
            if (comparer.Compare(check.Property, value) <= 0)
                return check;

         
            var err = error ?? check.Property.LessThanOrEqualError(
                value: value,
                isInvariant: check.IsInvariant,
                codePrefix: check.OwnerName,
                propertyName: check.PropertyName,
                field: check.FieldName,
                message: message);

            return check.Fail(err);
        }
    }

    // ---------------------------
    // Nullable comparable (struct?): validate only when has value
    // ---------------------------

    extension<TOwner, T>(CheckField<TOwner, T?> check) where T : struct, IComparable<T>
    {
        public CheckField<TOwner, T?> BetweenInclusiveIfHasValue(T min,
            T max,
            string? message = null,
            Error? error = null,
            IComparer<T>? comparer = null)
        {
            if (!check.ShouldContinue || !check.Property.HasValue) return check;

            comparer ??= Comparer<T>.Default;
            var v = check.Property.Value;

            if (comparer.Compare(v, min) >= 0 && comparer.Compare(v, max) <= 0)
                return check;

            var err = error ?? v.BetweenInclusiveError(
                min: min,
                max: max,
                isInvariant: check.IsInvariant,
                codePrefix: check.OwnerName,
                propertyName: check.PropertyName,
                field: check.FieldName,
                message: message);

            return check.Fail(err);
        }

        public CheckField<TOwner, T?> BetweenExclusiveIfHasValue(T min,
            T max,
            string? message = null,
            Error? error = null,
            IComparer<T>? comparer = null)
        {
            if (!check.ShouldContinue || !check.Property.HasValue) return check;

            comparer ??= Comparer<T>.Default;
            var v = check.Property.Value;

            if (comparer.Compare(v, min) > 0 && comparer.Compare(v, max) < 0)
                return check;

            var err = error ?? v.BetweenExclusiveError(
                min: min,
                max: max,
                isInvariant: check.IsInvariant,
                codePrefix: check.OwnerName,
                propertyName: check.PropertyName,
                field: check.FieldName,
                message: message);

            return check.Fail(err);
        }

        public CheckField<TOwner, T?> GreaterThanIfHasValue(T value,
            string? message = null,
            Error? error = null,
            IComparer<T>? comparer = null)
        {
            if (!check.ShouldContinue || !check.Property.HasValue) return check;

            comparer ??= Comparer<T>.Default;
            if (comparer.Compare(check.Property.Value, value) > 0) return check;

            var err = error ?? check.Property.Value.GreaterThanError(
                value: value,
                isInvariant: check.IsInvariant,
                codePrefix: check.OwnerName,
                propertyName: check.PropertyName,
                field: check.FieldName,
                message: message);

            return check.Fail(err);
        }

        public CheckField<TOwner, T?> GreaterThanOrEqualIfHasValue(T value,
            string? message = null,
            Error? error = null,
            IComparer<T>? comparer = null)
        {
            if (!check.ShouldContinue || !check.Property.HasValue) return check;

            comparer ??= Comparer<T>.Default;
            if (comparer.Compare(check.Property.Value, value) >= 0) return check;

            var err = error ?? check.Property.Value.GreaterThanOrEqualError(
                value: value,
                isInvariant: check.IsInvariant,
                codePrefix: check.OwnerName,
                propertyName: check.PropertyName,
                field: check.FieldName,
                message: message);

            return check.Fail(err);
        }

        public CheckField<TOwner, T?> LessThanIfHasValue(T value,
            string? message = null,
            Error? error = null,
            IComparer<T>? comparer = null)
        {
            if (!check.ShouldContinue || !check.Property.HasValue) return check;

            comparer ??= Comparer<T>.Default;
            if (comparer.Compare(check.Property.Value, value) < 0) return check;

            var err = error ?? check.Property.Value.LessThanError(
                value: value,
                isInvariant: check.IsInvariant,
                codePrefix: check.OwnerName,
                propertyName: check.PropertyName,
                field: check.FieldName,
                message: message);

            return check.Fail(err);
        }

        public CheckField<TOwner, T?> LessThanOrEqualIfHasValue(T value,
            string? message = null,
            Error? error = null,
            IComparer<T>? comparer = null)
        {
            if (!check.ShouldContinue || !check.Property.HasValue) return check;

            comparer ??= Comparer<T>.Default;
            if (comparer.Compare(check.Property.Value, value) <= 0) return check;

            var err = error ?? check.Property.Value.LessThanOrEqualError(
                value: (object)value!,
                isInvariant: check.IsInvariant,
                codePrefix: check.OwnerName,
                propertyName: check.PropertyName,
                field: check.FieldName,
                message: message);

            return check.Fail(err);
        }
    }
}