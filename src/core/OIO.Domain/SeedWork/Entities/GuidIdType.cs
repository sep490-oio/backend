namespace OIO.Domain.SeedWork.Entities;

public abstract record GuidIdType<TSelf> : StronglyTypedId<TSelf, Guid>
    where TSelf : GuidIdType<TSelf>, new()
{
    protected GuidIdType(Guid value) : base(value) {}

    protected GuidIdType() : base()
    {
    }

    public static TSelf Create()
    {
        GuidIdType<TSelf>  id = new TSelf();
        id.Value = Guid.CreateVersion7();
        return (TSelf)id;
    }

    public override string GetValueAsString()
    {
        return Value.ToString();
    }

    public override string ToString()
    {
        return Value.ToString();
    }
}