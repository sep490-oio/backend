namespace OIO.Domain.SeedWork.Entities;

public interface IEntityId
{
    string GetValueAsString();
}

public interface IEntityId<TSelf, TValue> : IEntityId
    where TSelf : IEntityId<TSelf, TValue>
    where TValue : notnull
{
    TValue Value { get; }
    static abstract TSelf From(TValue value);
}

public interface IInitializableId<in TIn, out TOut>
{
    TOut Initialize(TIn value);
}