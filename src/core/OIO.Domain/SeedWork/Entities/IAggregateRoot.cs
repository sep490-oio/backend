namespace OIO.Domain.SeedWork.Entities;

public interface IAggregateRoot;

public interface IAggregateRoot<TId> : IAggregateRoot, IEntity<TId>
    where TId : IEntityId;