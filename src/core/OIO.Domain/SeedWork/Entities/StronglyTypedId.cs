using CSharpFunctionalExtensions;

namespace OIO.Domain.SeedWork.Entities;

public abstract record StronglyTypedId<TSelf, TValue> 
    :  IEntityId<TSelf, TValue>, IComparable<TSelf>
    where TSelf : StronglyTypedId<TSelf, TValue>, new()
    where TValue : IComparable<TValue>, IComparable
{
    public TValue Value { get; protected set; }
    
    public static TSelf From(TValue value)
    {
        StronglyTypedId<TSelf, TValue> id = new TSelf();
        id.Value =  value;
        return (TSelf)id;
    }

    protected StronglyTypedId(TValue value)
    {
        Value = value;
    }

    protected StronglyTypedId()
    {
        Value = default!;
    }
   
    public abstract string GetValueAsString();
    
    public int CompareTo(TSelf? other)
        => other is null ? 1 : Value.CompareTo(other.Value);
}

