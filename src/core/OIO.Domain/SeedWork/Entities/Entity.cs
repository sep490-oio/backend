using CSharpFunctionalExtensions;

namespace OIO.Domain.SeedWork.Entities;

public abstract class BaseEntity<TId> : Entity<TId>, IEntity<TId>
    where TId : IEntityId, IComparable<TId>
{
    protected BaseEntity(TId id) : base(id) {}
    
    protected BaseEntity() : base() {}
}
