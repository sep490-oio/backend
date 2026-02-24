using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using CSharpFunctionalExtensions;
using OIO.Domain.SeedWork.Errors;
using OIO.Domain.SeedWork.Utils;

namespace OIO.Domain.SeedWork.Checks;

public sealed class CheckContext<TOwner>
{
    public CheckState State { get; }
    public bool IsInvariant { get; }
    public string OwnerName { get; private set; }
    public string Separator { get; }

    public CheckContext(CheckState state, bool isInvariant = false, string? separator = null)
    {
        State = state ?? throw new ArgumentNullException(nameof(state));
        IsInvariant = isInvariant;
        Separator = separator ?? Constant.DefaultErrorCodeSeparator;
        OwnerName = typeof(TOwner).Name;
    }

    public CheckContext<TOwner> WithOwnerName(params string[] ownerName)
    {
        OwnerName = string.Join(Separator, ownerName).ToTitleCase();
        return this;
    }

    public ViolationsError ToViolationsError(
        string? prefix = null,
        string? separator = null,
        string? suffix = null,
        string? message = null)
        => State.ToViolationsError(prefix ?? OwnerName, separator ?? Separator, suffix, message);
    
   
    
    public static implicit operator ViolationsError (CheckContext<TOwner> checkContext) => checkContext.ToViolationsError();
}

public readonly struct CheckField<TOwner, TProperty>
{
    public CheckState State { get; }
    public bool IsInvariant { get; }
    public string OwnerName { get; }
    public string PropertyName { get; }
    public string FieldName { get; }
    public string Separator { get; }
    public TProperty Property { get; }

    private readonly bool _fieldFailed;

    public string CodePrefix => $"{OwnerName}{Separator}{PropertyName}";

    // Global
    public bool IsFailure => State.IsFailure;
    public Error? Error => State.Error;
    public IReadOnlyList<Error> Errors => State.Errors;

    // Per-field flow control:
    // - StopOnFirst: follow global State.ShouldContinue
    // - CollectAll: stop only if THIS field already failed
    public bool ShouldContinue
        => IsCollectAll(State.Mode) ? !_fieldFailed : State.ShouldContinue;

    public bool FieldFailed => _fieldFailed;

    internal CheckField(
        TProperty property,
        CheckState state,
        bool isInvariant,
        string ownerName,
        string propertyName,
        string fieldName,
        string separator,
        bool fieldFailed)
    {
        Property = property;
        State = state;
        IsInvariant = isInvariant;
        OwnerName = ownerName;
        PropertyName = propertyName;
        FieldName = fieldName;
        Separator = separator;
        _fieldFailed = fieldFailed;
    }

    public CheckField<TOwner, TProperty> Fail(Error error)
    {
        State.Fail(error);
        return With(fieldFailed: true);
    }

    public CheckField<TOwner, TProperty> WithOwnerName(params string[] ownerName)
        => With(ownerName: string.Join(Separator, ownerName).ToTitleCase());

    public CheckField<TOwner, TProperty> WithPropertyName(string propertyName)
        => With(propertyName: propertyName.ToTitleCase());

    public CheckField<TOwner, TProperty> WithFieldName(string fieldName)
        => With(fieldName: fieldName.ToTitleCase());
    
 
    public CheckField<TOwner, TNewProperty> Field<TNewProperty>(
        TNewProperty newProperty,
        string? propertyName = null,
        string? fieldName = null,
        string? separator = null,
        [CallerArgumentExpression("newProperty")] string expr = "")
    {
        propertyName ??= expr.ExtractLastMember();
        var pn = propertyName.ToTitleCase();
        return new CheckField<TOwner, TNewProperty>(
            property: newProperty,
            state: State,
            isInvariant: IsInvariant,
            ownerName: OwnerName,
            propertyName: pn,
            fieldName: (fieldName?.ToTitleCase() ?? pn),
            separator: separator ?? Separator,
            fieldFailed: false
        );
    }
    
    public CheckField<TOwner, TNewProperty> Field<TNewProperty>(
        TNewProperty newProperty,
        Expression<Func<TOwner, TNewProperty>> exprPropertyName,
        string? propertyName = null,
        string? fieldName = null,
        string? separator = null)
    {
        propertyName ??= exprPropertyName.GetOrAddName();
        var pn = propertyName.ToTitleCase();

        return new CheckField<TOwner, TNewProperty>(
            property: newProperty,
            state: State,
            isInvariant: IsInvariant,
            ownerName: OwnerName,
            propertyName: pn,
            fieldName: (fieldName?.ToTitleCase() ?? pn),
            separator: separator ?? Separator,
            fieldFailed: false
        );
    }
    
    internal CheckField<TOwner, TNewProperty> Narrow<TNewProperty>(TNewProperty newProperty)
        => new(
            property: newProperty,
            state: State,
            isInvariant: IsInvariant,
            ownerName: OwnerName,
            propertyName: PropertyName,
            fieldName: FieldName,
            separator: Separator,
            fieldFailed: _fieldFailed
        );

    private CheckField<TOwner, TProperty> With(
        string? ownerName = null,
        string? propertyName = null,
        string? fieldName = null,
        string? separator = null,
        bool? fieldFailed = null)
        => new(
            property: Property,
            state: State,
            isInvariant: IsInvariant,
            ownerName: ownerName ?? OwnerName,
            propertyName: propertyName ?? PropertyName,
            fieldName: fieldName ?? FieldName,
            separator: separator ?? Separator,
            fieldFailed: fieldFailed ?? _fieldFailed
        );
    
    

    private static bool IsCollectAll(string mode)
        => string.Equals(mode, CheckMode.CollectAll, StringComparison.Ordinal);
    
    public ViolationsError ToViolationsError(
        string? prefix = null,
        string? separator = null,
        string? suffix = null,
        string? message = null)
        => State.ToViolationsError(prefix ?? OwnerName, separator ?? Separator, suffix, message);

    public static implicit operator ViolationsError(CheckField<TOwner, TProperty> field) => field.ToViolationsError();
}