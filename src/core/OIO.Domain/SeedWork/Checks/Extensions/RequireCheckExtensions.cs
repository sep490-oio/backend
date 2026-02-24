using System.Diagnostics.CodeAnalysis;
using FluentCheck.Errors;
using OIO.Domain.SeedWork.Errors;

// ReSharper disable All

namespace OIO.Domain.SeedWork.Checks.Extensions;

public static class RequiredCheckFieldExtensions
{
    #region NotNull (ref type) : T? -> T

    public static CheckField<TOwner, T> NotNull<TOwner, T>(
        this CheckField<TOwner, T?> check,
        string? message = null,
        Error? error = null)
        where T : class
    {
        if (!check.ShouldContinue)
            return check.Narrow<T>(default!);

        if (check.Property is not null) 
            return check.Narrow(check.Property!);
        
        var err = error ?? check.Property.NotNullError(
            isInvariant: check.IsInvariant,
            codePrefix: check.OwnerName,
            propertyName: check.PropertyName,
            field: check.FieldName,
            message: message);

        return check.Fail(err).Narrow<T>(default!);

    }

    #endregion

    #region Notnull (value type) : T? -> T

    public static CheckField<TOwner, T> NotNull<TOwner, T>(
        this CheckField<TOwner, T?> check,
        string? message = null,
        Error? error = null)
        where T : struct
    {
        if (!check.ShouldContinue)
            return check.Narrow<T>(default!);

        if (check.Property is not null) 
            return check.Narrow(check.Property.Value);
        
        var err = error ?? check.Property.NotNullError(
            isInvariant: check.IsInvariant,
            codePrefix: check.OwnerName,
            propertyName: check.PropertyName,
            field: check.FieldName,
            message: message);

        return check.Fail(err).Narrow<T>(default!);

    }

    #endregion

    #region NotEmpty (string) : string? -> string

    public static CheckField<TOwner, string> NotNullOrEmpty<TOwner>(
        this CheckField<TOwner, string?> check,
        string? message = null,
        Error? error = null)
    {
        if (!check.ShouldContinue)
            return check.Narrow(string.Empty);

        if (!string.IsNullOrEmpty(check.Property)) 
            return check.Narrow(check.Property!);
        
        var err = error ?? check.Property.NotEmptyError(
            isInvariant: check.IsInvariant,
            codePrefix: check.OwnerName,
            propertyName: check.PropertyName,
            field: check.FieldName,
            message: message);

        return check.Fail(err).Narrow(string.Empty);

    }

    public static CheckField<TOwner, string> NotEmpty<TOwner>(
        this CheckField<TOwner, string> check,
        string? message = null,
        Error? error = null)
    {
        if (!check.ShouldContinue || check.Property.Length != 0)
            return check;

        var err = error ?? check.Property.NotEmptyError(
            isInvariant: check.IsInvariant,
            codePrefix: check.OwnerName,
            propertyName: check.PropertyName,
            field: check.FieldName,
            message: message);

        return check.Fail(err);

    }

    #endregion

    #region NotBlank (string) : string? -> string

    public static CheckField<TOwner, string> NotNullOrWhiteSpace<TOwner>(
        this CheckField<TOwner, string?> check,
        string? message = null,
        Error? error = null)
    {
        if (!check.ShouldContinue)
            return check.Narrow(string.Empty);

        if (string.IsNullOrWhiteSpace(check.Property))
        {
            var err = error ?? check.Property.NotBlankError(
                isInvariant: check.IsInvariant,
                codePrefix: check.OwnerName,
                propertyName: check.PropertyName,
                field: check.FieldName,
                message: message);

            return check.Fail(err).Narrow(string.Empty);
        }

        return check.Narrow(check.Property!);
    }

    public static CheckField<TOwner, string> NotWhiteSpace<TOwner>(
        this CheckField<TOwner, string> check,
        string? message = null,
        Error? error = null)
    {
        if (!check.ShouldContinue)
            return check;

        if (string.IsNullOrWhiteSpace(check.Property))
        {
            var err = error ?? check.Property.NotBlankError(
                isInvariant: check.IsInvariant,
                codePrefix: check.OwnerName,
                propertyName: check.PropertyName,
                field: check.FieldName,
                message: message);

            return check.Fail(err);
        }

        return check;
    }

    #endregion

    #region NotEmptyGuid : Guid? -> Guid

    public static CheckField<TOwner, Guid> NotEmptyGuid<TOwner>(
        this CheckField<TOwner, Guid?> check,
        string? message = null,
        Error? error = null)
    {
        if (!check.ShouldContinue)
            return check.Narrow(Guid.Empty);

        if (!check.Property.HasValue || check.Property.Value == Guid.Empty)
        {
            var err = error ?? check.Property.NotEmptyGuidError(
                isInvariant: check.IsInvariant,
                codePrefix: check.OwnerName,
                propertyName: check.PropertyName,
                field: check.FieldName,
                message: message);

            return check.Fail(err).Narrow(Guid.Empty);
        }

        return check.Narrow(check.Property.Value);
    }

    public static CheckField<TOwner, Guid> NotEmptyGuid<TOwner>(
        this CheckField<TOwner, Guid> check,
        string? message = null,
        Error? error = null)
    {
        if (!check.ShouldContinue)
            return check;

        if (check.Property == Guid.Empty)
        {
            var err = error ?? check.Property.NotEmptyGuidError(
                isInvariant: check.IsInvariant,
                codePrefix: check.OwnerName,
                propertyName: check.PropertyName,
                field: check.FieldName,
                message: message);

            return check.Fail(err);
        }

        return check;
    }

    #endregion
    

    #region NotDefault (struct) : T? -> T, T -> T

    public static CheckField<TOwner, T> NotDefault<TOwner, T>(
        this CheckField<TOwner, T?> check,
        T? @default = null,
        string? message = null,
        Error? error = null)
        where T : struct
    {
        if (!check.ShouldContinue)
            return check.Narrow(default(T));

        var defaultValue = @default ?? default(T);

        if (!check.Property.HasValue || EqualityComparer<T>.Default.Equals(check.Property.Value, defaultValue))
        {
            var err = error ?? check.Property.NotDefaultError(
                @default: defaultValue,
                isInvariant: check.IsInvariant,
                codePrefix: check.OwnerName,
                propertyName: check.PropertyName,
                field: check.FieldName,
                message: message);

            return check.Fail(err).Narrow(default(T));
        }

        return check.Narrow(check.Property.Value);
    }

    public static CheckField<TOwner, T> NotDefault<TOwner, T>(
        this CheckField<TOwner, T> check,
        T? @default = null,
        string? message = null,
        Error? error = null)
        where T : struct
    {
        if (!check.ShouldContinue)
            return check;

        var defaultValue = @default ?? default(T);

        if (EqualityComparer<T>.Default.Equals(check.Property, defaultValue))
        {
            var err = error ?? check.Property.NotDefaultError(
                @default: defaultValue,
                isInvariant: check.IsInvariant,
                codePrefix: check.OwnerName,
                propertyName: check.PropertyName,
                field: check.FieldName,
                message: message);

            return check.Fail(err);
        }

        return check;
    }

    #endregion

    #region NotEmpty (collections) – optional nhưng hay dùng

    public static CheckField<TOwner, TCollection> NotEmpty<TOwner, TItem, TCollection>(
        this CheckField<TOwner, TCollection?> check,
        string? message = null,
        Error? error = null)
        where TCollection : class, IReadOnlyCollection<TItem>
    {
        if (!check.ShouldContinue)
            return check.Narrow(default(TCollection)!);

        if (check.Property is null || check.Property.Count == 0)
        {
            var err = error ?? check.Property.NotEmptyError(
                isInvariant: check.IsInvariant,
                codePrefix: check.OwnerName,
                propertyName: check.PropertyName,
                field: check.FieldName,
                message: message);

            return check.Fail(err).Narrow(default(TCollection)!);
        }

        return check.Narrow(check.Property);
    }

    public static CheckField<TOwner, T[]> NotEmpty<TOwner, T>(
        this CheckField<TOwner, T[]?> check,
        string? message = null,
        Error? error = null)
    {
        if (!check.ShouldContinue)
            return check.Narrow(Array.Empty<T>());

        if (check.Property is null || check.Property.Length == 0)
        {
            var err = error ?? check.Property.NotEmptyError(
                isInvariant: check.IsInvariant,
                codePrefix: check.OwnerName,
                propertyName: check.PropertyName,
                field: check.FieldName,
                message: message);

            return check.Fail(err).Narrow(Array.Empty<T>());
        }

        return check.Narrow(check.Property);
    }
    #endregion
}