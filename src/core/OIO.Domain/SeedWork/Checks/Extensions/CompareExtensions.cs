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
            if (!check.ShouldContinue)
                return check;

            comparer ??= EqualityComparer<T>.Default;
            
            if (comparer.Equals(check.Property, value))
                return check;

            var err = error ?? Error.Equal(
                property: check.Property,
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
            if (!check.ShouldContinue)
                return check;

            comparer ??= EqualityComparer<T>.Default;
            
            if (!comparer.Equals(check.Property, value)) 
                return check;

            var err = error ?? Error.NotEqual(
                property: check.Property,
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
            if (!check.ShouldContinue) 
                return check;

            comparer ??= Comparer<T>.Default;

            if (comparer.Compare(check.Property, min) >= 0 && comparer.Compare(check.Property, max) <= 0)
                return check;

            var err = error ?? Error.BetweenInclusive(
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

        public CheckField<TOwner, T> BetweenExclusive(T min,
            T max,
            string? message = null,
            Error? error = null,
            IComparer<T>? comparer = null)
        {
            if (!check.ShouldContinue)
                return check;

            comparer ??= Comparer<T>.Default;

            if (comparer.Compare(check.Property, min) > 0 && comparer.Compare(check.Property, max) < 0)
                return check;

            var err = error ?? Error.BetweenExclusive(
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

        public CheckField<TOwner, T> GreaterThan(T value,
            string? message = null,
            Error? error = null,
            IComparer<T>? comparer = null)
        {
            if (!check.ShouldContinue)
                return check;

            comparer ??= Comparer<T>.Default;
            
            if (comparer.Compare(check.Property, value) > 0)
                return check;

            var err = error ?? Error.GreaterThan(
                property: check.Property,
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

            var err = error ?? Error.GreaterThanOrEqual(
                property: check.Property,
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
            if (!check.ShouldContinue)
                return check;

            comparer ??= Comparer<T>.Default;
            
            if (comparer.Compare(check.Property, value) < 0) 
                return check;

            var err = error ?? Error.LessThan(
                property: check.Property,
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
            if (!check.ShouldContinue)
                return check;

            comparer ??= Comparer<T>.Default;
            
            if (comparer.Compare(check.Property, value) <= 0)
                return check;

         
            var err = error ?? Error.LessThanOrEqual(
                property: check.Property,
                value: value,
                isInvariant: check.IsInvariant,
                codePrefix: check.OwnerName,
                propertyName: check.PropertyName,
                field: check.FieldName,
                message: message);

            return check.Fail(err);
        }
    }
}