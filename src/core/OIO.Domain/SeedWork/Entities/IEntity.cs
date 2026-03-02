namespace OIO.Domain.SeedWork.Entities;

public interface IEntity;

public interface IEntity<TId> : IEntity
    where TId : IEntityId
{
    TId Id { get; }
}

public interface ISoftDeletableEntity
{
    DateTime? DeletedAt { get; }
    bool IsDeleted { get; }
    void SoftDelete(DateTime deletedAt);
}

public interface IAuditableEntity : ICreatedAtEntity, IModifiedAtEntity;

public interface ICreatedAtEntity
{
    DateTime CreatedAt { get; }
}

public interface IModifiedAtEntity
{
    DateTime? ModifiedAt { get; }
}

public interface IVersionEntity
{
    int Version { get; }
}