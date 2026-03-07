using CSharpFunctionalExtensions;
using OIO.Domain.SeedWork.Checks;
using OIO.Domain.SeedWork.Utils;
using OIO.Domain.SeedWork.Errors.ErrorCatalogs;

namespace OIO.Domain.SeedWork.Errors;

[GenerateSerializer]
public record ViolationsError : Error
{
    [Id(0)]
    private readonly List<Error> _violation = [];
    
    public IReadOnlyList<Error> Violations => _violation;

    public bool IsEmpty => _violation.Count == 0;
    
    public bool HasErrors => !IsEmpty;
    
    public ViolationsError(
        string? prefix = null,
        string? separator = null,
        string? suffix = null,
        string? message = null,
        params Error[] errors) 
        : base(
            code: BuildCode(prefix, separator, suffix),
            message: message ?? Constant.DefaultViolationsMessage,
            kind: ErrorCatalog.Kind.Violations)
    { 
        AddRange(errors);
    }
    
    public ViolationsError Add(Error error)
    {
        switch (error)
        {
            case ViolationsError violationsError:
                _violation.AddRange(violationsError.Violations);
                break;
            case ICheckError:
                _violation.Add(error);
                break;
        }

        return this;
    }

    public ViolationsError AddRange(params Error[] errors)
    {
        if (errors.Length == 0)
            return this;

        foreach (var error in errors)
        {
            Add(error);
        }

        return this;
    }
    
    
    private static string BuildCode(string? prefix, string? separator, string? suffix)
    {
        prefix ??= Constant.DefaultErrorPrefix;
        separator ??= Constant.DefaultErrorCodeSeparator; 
        suffix ??= ErrorCatalog.Kind.Violations;
        return prefix + separator + suffix;
    }
}

public static class ViolationsErrorExtensions
{
    extension(ViolationsError error)
    {
        public ViolationsError From<T>(params T[] results) where T : IResult, IError<Error>
        {
            foreach (var r in results)
            {
                if (r.IsSuccess) continue;

                error.Add(r.Error);
            }

            return error;
        }
     
        public static ViolationsError FromResults<T>(
            string? prefix = null,
            string? separator = null,
            string? suffix = null,
            string? message = null,
            params T[] results) where T : IResult, IError<Error>
        {
            var vr = new ViolationsError(prefix, separator, suffix, message);
            vr.From(results);
            return vr;
        }
    }
}