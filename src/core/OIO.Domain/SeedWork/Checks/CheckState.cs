using OIO.Domain.SeedWork.Errors;

namespace OIO.Domain.SeedWork.Checks;

public interface ICheckError
{
    public string Code { get; }
    public string Message { get; }
    public string PropertyName { get; }
}

public class CheckState
{
    public readonly string Mode;
    private Error? _error;
    private List<Error>? _errors;

    public CheckState(string mode)
    {
        Mode = mode;
    }
    
    public virtual bool IsFailure => _error is not null || _errors is { Count: > 0 };
    
    public virtual Error? Error => _error ?? _errors?.FirstOrDefault();
    
    public virtual IReadOnlyList<Error> Errors
    {
        get
        {
            if (Mode == CheckMode.CollectAll && _errors is not null)
                return _errors;

            return _error is null ? [] : new[] { _error };
        }
    }

    public virtual bool ShouldContinue => Mode == CheckMode.CollectAll || !IsFailure;
    
    public virtual void Fail(Error error)
    {
        if (error is ViolationsError violationsError)
        {
            (_errors ??= new List<Error>(4)).AddRange(violationsError.Violations);
        }
        
        if (error is not ICheckError)
        {
            return;
        }
        
        if (Mode == CheckMode.StopOnFirst)
        {
            _error ??= error;
            return;
        }

        (_errors ??= new List<Error>(4)).Add(error);
    }
    
    public virtual ViolationsError ToViolationsError(
        string? prefix = null,
        string? separator = null,
        string? suffix = null,
        string? message = null)
    {

        var vr = new ViolationsError(prefix, separator, suffix, message);
        
        if (_error is not null) 
            vr.Add(_error);
        
        if (_errors is { Count: > 0 })
            vr.AddRange(_errors.ToArray());

        return vr;
    }
    
    public static implicit operator ViolationsError (CheckState checkState) => checkState.ToViolationsError(); 
}