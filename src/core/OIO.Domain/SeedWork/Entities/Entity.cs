namespace OIO.Domain.SeedWork.Entities;

public abstract class Entity<TId> : CSharpFunctionalExtensions.Entity<TId>, IEntity<TId>
    where TId : IEntityId, IComparable<TId>
{
    protected Entity(TId id) : base(id) {}
    
    protected Entity() : base() {}
}
