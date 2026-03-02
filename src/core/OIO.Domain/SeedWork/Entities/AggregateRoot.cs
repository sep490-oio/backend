using CSharpFunctionalExtensions;
using OIO.Domain.SeedWork.DomainEvents;

namespace OIO.Domain.SeedWork.Entities;

public abstract class AggregateRoot<TId> : BaseEntity<TId>, IAggregateRoot<TId>, IHasDomainEvents
    where TId : IEntityId, IComparable<TId>
{
    protected AggregateRoot(TId id) : base(id)
    {
    }

    protected AggregateRoot() : base()
    {
    }
    
    private readonly List<IDomainEvent> _domainEvents = [];

    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
        
    protected void RaiseDomainEvent(params IDomainEvent[] eventItem)
    {
        _domainEvents.AddRange(eventItem);
    }
}